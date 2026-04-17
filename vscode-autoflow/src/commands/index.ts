import * as vscode from 'vscode';
import * as path from 'path';
import { exec } from 'child_process';
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
    const command = `dotnet run --project "${projectPath}" -- run "${filePath}" --output "${outputPath}"`;
    
    if (showOutput) {
        channel.show(true);
    }
    
    channel.appendLine(`Running workflow: ${filePath}`);
    channel.appendLine(`Command: ${command}`);
    channel.appendLine('---');

    statusBar?.setRunning();

    vscode.window.withProgress({
        location: vscode.ProgressLocation.Notification,
        title: 'Running AutoFlow workflow...',
        cancellable: false
    }, () => {
        return new Promise<void>((resolve) => {
            exec(command, { maxBuffer: 1024 * 1024 * 10 }, (error, stdout, stderr) => {
                channel.appendLine(stdout);
                
                if (stderr) {
                    channel.appendLine('STDERR:');
                    channel.appendLine(stderr);
                }
                
                if (error) {
                    channel.appendLine(`\nError: ${error.message}`);
                    vscode.window.showErrorMessage(`Workflow failed: ${error.message}`);
                    statusBar?.setFailed(error.message);
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

    const command = `dotnet run --project "${projectPath}" -- validate "${filePath}"`;

    vscode.window.withProgress({
        location: vscode.ProgressLocation.Notification,
        title: 'Validating workflow...',
        cancellable: false
    }, () => {
        return new Promise<void>((resolve) => {
            exec(command, { maxBuffer: 1024 * 1024 }, (error, stdout, stderr) => {
                const output = stdout + stderr;
                
                if (error) {
                    vscode.window.showErrorMessage(`Validation failed:\n${output}`);
                } else {
                    vscode.window.showInformationMessage('✓ Workflow is valid!');
                }
                
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

    const statusArg = selectedStatus === 'All' ? '' : `--status ${selectedStatus}`;
    const command = `dotnet run --project "${projectPath}" -- history ${statusArg}`;

    const channel = getOutputChannel();
    channel.show(true);
    channel.appendLine(`Execution History (${selectedStatus}):`);
    channel.appendLine('---');

    exec(command, { maxBuffer: 1024 * 1024 }, (error, stdout, stderr) => {
        channel.appendLine(stdout);
        if (stderr) {
            channel.appendLine(stderr);
        }
        if (error) {
            channel.appendLine(`Error: ${error.message}`);
        }
    });
}

async function listKeywords(): Promise<void> {
    const projectPath = getProjectPath();
    if (!projectPath) {
        vscode.window.showErrorMessage('Could not find AutoFlow.Cli project.');
        return;
    }

    const command = `dotnet run --project "${projectPath}" -- list-keywords`;

    const channel = getOutputChannel();
    channel.show(true);
    channel.appendLine('Available Keywords:');
    channel.appendLine('---');

    exec(command, { maxBuffer: 1024 * 1024 }, (error, stdout, stderr) => {
        channel.appendLine(stdout);
        if (stderr) {
            channel.appendLine(stderr);
        }
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

    const command = `dotnet run --project "${projectPath}" -- stats --days ${days}`;

    const channel = getOutputChannel();
    channel.show(true);
    channel.appendLine(`AutoFlow Statistics (last ${days} days):`);
    channel.appendLine('---');

    exec(command, { maxBuffer: 1024 * 1024 }, (error, stdout, stderr) => {
        channel.appendLine(stdout);
        if (stderr) {
            channel.appendLine(stderr);
        }
        if (error) {
            channel.appendLine(`Error: ${error.message}`);
        }
    });
}
