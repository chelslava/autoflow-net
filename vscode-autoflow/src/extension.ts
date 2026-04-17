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

export function activate(context: vscode.ExtensionContext) {
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

    const treeDataProvider = new AutoFlowTreeDataProvider();
    context.subscriptions.push(
        vscode.window.registerTreeDataProvider('autoflow-explorer', treeDataProvider)
    );

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

    context.subscriptions.push(
        vscode.window.onDidChangeActiveTextEditor(editor => {
            if (editor && (editor.document.languageId === 'autoflow' || 
                editor.document.fileName.endsWith('.yaml') || 
                editor.document.fileName.endsWith('.yml'))) {
                treeDataProvider.refresh(editor.document);
            }
        })
    );

    context.subscriptions.push(
        vscode.workspace.onDidChangeTextDocument(e => {
            if (e.document.languageId === 'autoflow' || 
                e.document.fileName.endsWith('.yaml') || 
                e.document.fileName.endsWith('.yml')) {
                treeDataProvider.refresh(e.document);
            }
        })
    );

    registerCommands(context, statusBar);

    if (vscode.window.activeTextEditor) {
        treeDataProvider.refresh(vscode.window.activeTextEditor.document);
    }
}

export function deactivate() {}
