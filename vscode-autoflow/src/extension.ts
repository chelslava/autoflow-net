import * as vscode from 'vscode';
import { AutoFlowCompletionProvider } from './providers/completionProvider';
import { AutoFlowHoverProvider } from './providers/hoverProvider';
import { AutoFlowDiagnosticsProvider } from './providers/diagnosticsProvider';
import { AutoFlowCodeActionProvider } from './providers/codeActionProvider';
import { AutoFlowDefinitionProvider } from './providers/definitionProvider';
import { AutoFlowReferenceProvider } from './providers/referenceProvider';
import { AutoFlowFoldingProvider } from './providers/foldingProvider';
import { AutoFlowDocumentLinkProvider } from './providers/documentLinkProvider';
import { AutoFlowSignatureHelpProvider } from './providers/signatureHelpProvider';
import { AutoFlowWorkspaceSymbolProvider } from './providers/workspaceSymbolProvider';
import { AutoFlowTreeDataProvider } from './providers/treeViewProvider';
import { AutoFlowStatusBar } from './providers/statusBar';
import { registerCommands } from './commands';
import { AutoFlowDebugAdapterDescriptorFactory } from './debug/debugAdapter';

let providersRegistered = false;

export function activate(context: vscode.ExtensionContext) {
    const statusBar = new AutoFlowStatusBar();
    context.subscriptions.push(statusBar);

    const debugAdapterFactory = new AutoFlowDebugAdapterDescriptorFactory();
    context.subscriptions.push(
        vscode.debug.registerDebugAdapterDescriptorFactory('autoflow', debugAdapterFactory)
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('autoflow.showStatus', () => statusBar.showQuickPick())
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('autoflow.gotoLine', (line: number) => {
            const editor = vscode.window.activeTextEditor;
            if (editor) {
                const position = new vscode.Position(line, 0);
                editor.selection = new vscode.Selection(position, position);
                editor.revealRange(new vscode.Range(position, position), vscode.TextEditorRevealType.InCenter);
            }
        })
    );

    if (hasAutoFlowDocument()) {
        registerProviders(context);
    }

    context.subscriptions.push(
        vscode.workspace.onDidOpenTextDocument(doc => {
            if (isAutoFlowDocument(doc) && !providersRegistered) {
                registerProviders(context);
            }
        })
    );

    context.subscriptions.push(
        vscode.window.onDidChangeActiveTextEditor(editor => {
            if (editor && isAutoFlowDocument(editor.document) && !providersRegistered) {
                registerProviders(context);
            }
            if (editor && isAutoFlowDocument(editor.document)) {
                const treeDataProvider = getTreeDataProvider(context);
                treeDataProvider.refresh(editor.document);
            }
        })
    );

    registerCommands(context, statusBar);
    
    context.subscriptions.push(
        vscode.commands.registerCommand('autoflow.copyStepRef', (item) => {
            if (item?.stepId) {
                vscode.env.clipboard.writeText(`\${steps.${item.stepId}.outputs}`);
                vscode.window.showInformationMessage(`Copied: \${steps.${item.stepId}.outputs}`);
            }
        })
    );
    
    context.subscriptions.push(
        vscode.commands.registerCommand('autoflow.runFromStep', (item) => {
            vscode.window.showInformationMessage('Run from step requires workflow modification. Use the run command to execute the full workflow.');
        })
    );
    
    context.subscriptions.push(
        vscode.commands.registerCommand('autoflow.newWorkflow', async () => {
            const templates = ['basic', 'http', 'browser', 'parallel', 'files'];
            const selected = await vscode.window.showQuickPick(templates, {
                placeHolder: 'Select workflow template'
            });
            
            if (!selected) return;
            
            const name = await vscode.window.showInputBox({
                prompt: 'Workflow name',
                value: 'my_workflow'
            });
            
            if (!name) return;
            
            const fileName = name.endsWith('.yaml') ? name : `${name}.yaml`;
            const content = getTemplateContent(selected);
            
            const workspaceFolder = vscode.workspace.workspaceFolders?.[0];
            if (!workspaceFolder) {
                vscode.window.showErrorMessage('No workspace folder open');
                return;
            }
            
            const filePath = vscode.Uri.joinPath(workspaceFolder.uri, fileName);
            await vscode.workspace.fs.writeFile(filePath, Buffer.from(content, 'utf-8'));
            
            const doc = await vscode.workspace.openTextDocument(filePath);
            await vscode.window.showTextDocument(doc);
            
            vscode.window.showInformationMessage(`Created: ${fileName}`);
        })
    );
}

function isAutoFlowDocument(doc: vscode.TextDocument): boolean {
    return doc.languageId === 'autoflow' || 
           doc.fileName.endsWith('.yaml') || 
           doc.fileName.endsWith('.yml');
}

function hasAutoFlowDocument(): boolean {
    return vscode.workspace.textDocuments.some(isAutoFlowDocument);
}

let treeDataProvider: AutoFlowTreeDataProvider | undefined;

function getTreeDataProvider(context: vscode.ExtensionContext): AutoFlowTreeDataProvider {
    if (!treeDataProvider) {
        treeDataProvider = new AutoFlowTreeDataProvider();
        context.subscriptions.push(
            vscode.window.registerTreeDataProvider('autoflow-explorer', treeDataProvider)
        );

        context.subscriptions.push(
            vscode.workspace.onDidChangeTextDocument(e => {
                if (isAutoFlowDocument(e.document)) {
                    treeDataProvider!.refresh(e.document);
                }
            })
        );
    }
    return treeDataProvider;
}

function registerProviders(context: vscode.ExtensionContext) {
    if (providersRegistered) return;
    providersRegistered = true;

    const selector = { language: 'autoflow' };

    const completionProvider = new AutoFlowCompletionProvider();
    context.subscriptions.push(
        vscode.languages.registerCompletionItemProvider(selector, completionProvider, '.', ' ', ':')
    );

    const hoverProvider = new AutoFlowHoverProvider();
    context.subscriptions.push(
        vscode.languages.registerHoverProvider(selector, hoverProvider)
    );

    const diagnosticsProvider = new AutoFlowDiagnosticsProvider();
    context.subscriptions.push(diagnosticsProvider);

    context.subscriptions.push(
        vscode.languages.registerCodeActionsProvider(selector, new AutoFlowCodeActionProvider(), {
            providedCodeActionKinds: AutoFlowCodeActionProvider.providedCodeActionKinds
        })
    );

    context.subscriptions.push(
        vscode.languages.registerDefinitionProvider(selector, new AutoFlowDefinitionProvider())
    );

    context.subscriptions.push(
        vscode.languages.registerReferenceProvider(selector, new AutoFlowReferenceProvider())
    );

    context.subscriptions.push(
        vscode.languages.registerFoldingRangeProvider(selector, new AutoFlowFoldingProvider())
    );

    context.subscriptions.push(
        vscode.languages.registerDocumentLinkProvider(selector, new AutoFlowDocumentLinkProvider())
    );

    context.subscriptions.push(
        vscode.languages.registerSignatureHelpProvider(
            selector, 
            new AutoFlowSignatureHelpProvider(), 
            ' ', ':', ','
        )
    );

    context.subscriptions.push(
        vscode.languages.registerWorkspaceSymbolProvider(new AutoFlowWorkspaceSymbolProvider())
    );

    const tdProvider = getTreeDataProvider(context);
    
    if (vscode.window.activeTextEditor && isAutoFlowDocument(vscode.window.activeTextEditor.document)) {
        tdProvider.refresh(vscode.window.activeTextEditor.document);
    }
}

function getTemplateContent(template: string): string {
    const templates: Record<string, string> = {
        basic: `schema_version: 1
name: basic_workflow

variables:
  message: Hello, World!

tasks:
  main:
    steps:
      - step:
          id: log_message
          uses: log.info
          with:
            message: "\${message}"
`,
        http: `schema_version: 1
name: http_workflow

variables:
  api_base: https://api.example.com

tasks:
  main:
    steps:
      - step:
          id: fetch_data
          uses: http.request
          with:
            url: "\${api_base}/data"
            method: GET
          save_as:
            body: response_data
`,
        browser: `schema_version: 1
name: browser_workflow

tasks:
  main:
    steps:
      - step:
          id: open_browser
          uses: browser.open
          with:
            browser: chromium
            headless: true
          save_as:
            browserId: browser_id

      - step:
          id: navigate
          uses: browser.goto
          with:
            browserId: "\${browser_id}"
            url: https://example.com

      - step:
          id: close
          uses: browser.close
          with:
            browserId: "\${browser_id}"
`,
        parallel: `schema_version: 1
name: parallel_workflow

tasks:
  main:
    steps:
      - parallel:
          id: fetch_all
          max_concurrency: 3
          steps:
            - step:
                id: fetch_users
                uses: http.request
                with:
                  url: https://api.example.com/users
`,
        files: `schema_version: 1
name: files_workflow

tasks:
  main:
    steps:
      - step:
          id: read_file
          uses: files.read
          with:
            path: ./input.txt
          save_as:
            content: file_content

      - step:
          id: write_file
          uses: files.write
          with:
            path: ./output.txt
            content: "\${file_content}"
`
    };
    
    return templates[template] || templates.basic;
}

export function deactivate() {}
