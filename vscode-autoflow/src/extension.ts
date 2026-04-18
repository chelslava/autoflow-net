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

let providersRegistered = false;

export function activate(context: vscode.ExtensionContext) {
    const statusBar = new AutoFlowStatusBar();
    context.subscriptions.push(statusBar);

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

export function deactivate() {}
