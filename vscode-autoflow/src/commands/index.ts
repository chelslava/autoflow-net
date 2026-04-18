import * as vscode from 'vscode';
import * as path from 'path';
import { spawn } from 'child_process';
import type { AutoFlowStatusBar } from '../providers/statusBar';

export function registerCommands(context: vscode.ExtensionContext, statusBar?: AutoFlowStatusBar): void {
    context.subscriptions.push(
        vscode.commands.registerCommand('autoflow.run', () => runWorkflow(statusBar)),
        vscode.commands.registerCommand('autoflow.validate', validateWorkflow),
        vscode.commands.registerCommand('autoflow.history', showHistory),
        vscode.commands.registerCommand('autoflow.listKeywords', listKeywords),
        vscode.commands.registerCommand('autoflow.stats', showStats)
    );
}

let outputChannel: vscode.OutputChannel | undefined;

function getOutputChannel(): vscode.OutputChannel {
    if (!outputChannel) {
        outputChannel = vscode.window.createOutputChannel('AutoFlow');
    }
    return outputChannel;
}

function getProjectPath(): string | null {
    const config = vscode.workspace.getConfiguration('autoflow');
    const configuredPath = config.get<string>('projectPath');
    
    if (configuredPath) {
        return configuredPath;
    }

    const workspaceFolders = vscode.workspace.workspaceFolders;
    if (!workspaceFolders || workspaceFolders.length === 0) {
        return null;
    }

    const workspaceRoot = workspaceFolders[0].uri.fsPath;
    const possiblePaths = [
        path.join(workspaceRoot, 'src', 'AutoFlow.Cli'),
        path.join(workspaceRoot, 'AutoFlow.Cli'),
        workspaceRoot
    ];

    for (const p of possiblePaths) {
        const projFile = path.join(p, 'AutoFlow.Cli.csproj');
        try {
            require('fs').accessSync(projFile);
            return p;
        } catch {}
    }

    return workspaceRoot;
}

async function runWorkflow(statusBar?: AutoFlowStatusBar): Promise<void> {
    const editor = vscode.window.activeTextEditor;
    if (!editor) {
        vscode.window.showErrorMessage('No active editor. Open a workflow YAML file first.');
        return;
    }

    const filePath = editor.document.uri.fsPath;
    if (!filePath.endsWith('.yaml') && !filePath.endsWith('.yml')) {
        vscode.window.showErrorMessage('Current file is not a YAML workflow file.');
        return;
    }

    const projectPath = getProjectPath();
    if (!projectPath) {
        vscode.window.showErrorMessage('Could not find AutoFlow.Cli project. Configure autoflow.projectPath setting.');
        return;
    }

    const config = vscode.workspace.getConfiguration('autoflow');
    const showOutput = config.get<boolean>('showOutputOnRun', true);
    const outputFormat = config.get<string>('outputFormat', 'json');

    const channel = getOutputChannel();
    
    const outputPath = filePath.replace(/\.(ya?ml)$/, `.${outputFormat === 'html' ? 'html' : 'json'}`);
    
    if (showOutput) {
        channel.show(true);
    }
    
    channel.appendLine(`Running workflow: ${filePath}`);
    channel.appendLine(`Output: ${outputPath}`);
    channel.appendLine('---');

    statusBar?.setRunning();

    vscode.window.withProgress({
        location: vscode.ProgressLocation.Notification,
        title: 'Running AutoFlow workflow...',
        cancellable: false
    }, () => {
        return new Promise<void>((resolve) => {
            const child = spawn('dotnet', [
                'run',
                '--project', projectPath,
                '--',
                'run',
                filePath,
                '--output', outputPath
            ], {
                cwd: projectPath,
                shell: false
            });

            let stdout = '';
            let stderr = '';

            child.stdout.on('data', (data) => {
                const text = data.toString();
                stdout += text;
                channel.append(text);
            });

            child.stderr.on('data', (data) => {
                const text = data.toString();
                stderr += text;
                channel.append(text);
            });

            child.on('close', (code) => {
                if (code !== 0) {
                    channel.appendLine(`\nError: Process exited with code ${code}`);
                    vscode.window.showErrorMessage(`Workflow failed (exit code ${code})`);
                    statusBar?.setFailed(`Exit code ${code}`);
                } else {
                    channel.appendLine('\n✓ Workflow completed successfully');
                    vscode.window.showInformationMessage('Workflow completed successfully!');
                    statusBar?.setSuccess();
                    
                    if (outputFormat === 'html') {
                        vscode.window.showInformationMessage('Open HTML Report?', 'Open', 'Dismiss').then(selection => {
                            if (selection === 'Open') {
                                vscode.commands.executeCommand('vscode.open', vscode.Uri.file(outputPath));
                            }
                        });
                    }
                }
                
                resolve();
            });

            child.on('error', (error) => {
                channel.appendLine(`\nError: ${error.message}`);
                vscode.window.showErrorMessage(`Failed to run workflow: ${error.message}`);
                statusBar?.setFailed(error.message);
                resolve();
            });
        });
    });
}

async function validateWorkflow(): Promise<void> {
    const editor = vscode.window.activeTextEditor;
    if (!editor) {
        vscode.window.showErrorMessage('No active editor. Open a workflow YAML file first.');
        return;
    }

    const filePath = editor.document.uri.fsPath;
    if (!filePath.endsWith('.yaml') && !filePath.endsWith('.yml')) {
        vscode.window.showErrorMessage('Current file is not a YAML workflow file.');
        return;
    }

    const projectPath = getProjectPath();
    if (!projectPath) {
        vscode.window.showErrorMessage('Could not find AutoFlow.Cli project.');
        return;
    }

    vscode.window.withProgress({
        location: vscode.ProgressLocation.Notification,
        title: 'Validating workflow...',
        cancellable: false
    }, () => {
        return new Promise<void>((resolve) => {
            const child = spawn('dotnet', [
                'run',
                '--project', projectPath,
                '--',
                'validate',
                filePath
            ], {
                cwd: projectPath,
                shell: false
            });

            let stdout = '';
            let stderr = '';

            child.stdout.on('data', (data) => {
                stdout += data.toString();
            });

            child.stderr.on('data', (data) => {
                stderr += data.toString();
            });

            child.on('close', (code) => {
                const output = stdout + stderr;
                
                if (code !== 0) {
                    vscode.window.showErrorMessage(`Validation failed:\n${output}`);
                } else {
                    vscode.window.showInformationMessage('✓ Workflow is valid!');
                }
                
                resolve();
            });

            child.on('error', (error) => {
                vscode.window.showErrorMessage(`Failed to validate: ${error.message}`);
                resolve();
            });
        });
    });
}

async function showHistory(): Promise<void> {
    const projectPath = getProjectPath();
    if (!projectPath) {
        vscode.window.showErrorMessage('Could not find AutoFlow.Cli project.');
        return;
    }

    const statusOptions = ['All', 'Success', 'Failed', 'Running'];
    const selectedStatus = await vscode.window.showQuickPick(statusOptions, {
        placeHolder: 'Select execution status filter'
    });

    if (!selectedStatus) {
        return;
    }

    const args = ['run', '--project', projectPath, '--', 'history'];
    if (selectedStatus !== 'All') {
        args.push('--status', selectedStatus);
    }

    const channel = getOutputChannel();
    channel.show(true);
    channel.appendLine(`Execution History (${selectedStatus}):`);
    channel.appendLine('---');

    const child = spawn('dotnet', args, {
        cwd: projectPath,
        shell: false
    });

    child.stdout.on('data', (data) => {
        channel.append(data.toString());
    });

    child.stderr.on('data', (data) => {
        channel.append(data.toString());
    });

    child.on('error', (error) => {
        channel.appendLine(`Error: ${error.message}`);
    });
}

async function listKeywords(): Promise<void> {
    const projectPath = getProjectPath();
    if (!projectPath) {
        vscode.window.showErrorMessage('Could not find AutoFlow.Cli project.');
        return;
    }

    const channel = getOutputChannel();
    channel.show(true);
    channel.appendLine('Available Keywords:');
    channel.appendLine('---');

    const child = spawn('dotnet', [
        'run',
        '--project', projectPath,
        '--',
        'list-keywords'
    ], {
        cwd: projectPath,
        shell: false
    });

    child.stdout.on('data', (data) => {
        channel.append(data.toString());
    });

    child.stderr.on('data', (data) => {
        channel.append(data.toString());
    });

    child.on('error', (error) => {
        channel.appendLine(`Error: ${error.message}`);
    });
}

async function showStats(): Promise<void> {
    const projectPath = getProjectPath();
    if (!projectPath) {
        vscode.window.showErrorMessage('Could not find AutoFlow.Cli project.');
        return;
    }

    const days = await vscode.window.showInputBox({
        prompt: 'Number of days to include in statistics',
        value: '7',
        validateInput: (value) => {
            const num = parseInt(value, 10);
            if (isNaN(num) || num < 1) {
                return 'Please enter a positive number';
            }
            return null;
        }
    });

    if (!days) {
        return;
    }

    const channel = getOutputChannel();
    channel.show(true);
    channel.appendLine(`AutoFlow Statistics (last ${days} days):`);
    channel.appendLine('---');

    const child = spawn('dotnet', [
        'run',
        '--project', projectPath,
        '--',
        'stats',
        '--days', days
    ], {
        cwd: projectPath,
        shell: false
    });

    child.stdout.on('data', (data) => {
        channel.append(data.toString());
    });

    child.stderr.on('data', (data) => {
        channel.append(data.toString());
    });

    child.on('error', (error) => {
        channel.appendLine(`Error: ${error.message}`);
    });
}
