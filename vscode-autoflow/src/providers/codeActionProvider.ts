import * as vscode from 'vscode';

export class AutoFlowCodeActionProvider implements vscode.CodeActionProvider {
    public static readonly providedCodeActionKinds = [
        vscode.CodeActionKind.QuickFix
    ];

    provideCodeActions(
        document: vscode.TextDocument,
        range: vscode.Range | vscode.Selection,
        context: vscode.CodeActionContext,
        token: vscode.CancellationToken
    ): vscode.ProviderResult<(vscode.CodeAction | vscode.Command)[]> {
        const actions: vscode.CodeAction[] = [];

        for (const diagnostic of context.diagnostics) {
            if (diagnostic.message.includes('Missing schema_version')) {
                actions.push(this.createSchemaVersionFix(document));
            }
            if (diagnostic.message.includes('Missing workflow name')) {
                actions.push(this.createNameFix(document));
            }
            if (diagnostic.message.includes('Missing tasks section')) {
                actions.push(this.createTasksFix(document));
            }
            if (diagnostic.message.includes('Unknown keyword')) {
                const keyword = this.extractUnknownKeyword(diagnostic.message);
                if (keyword) {
                    actions.push(this.createKeywordSuggestion(document, diagnostic.range, keyword));
                }
            }
        }

        return actions;
    }

    private createSchemaVersionFix(document: vscode.TextDocument): vscode.CodeAction {
        const action = new vscode.CodeAction(
            'Add schema_version: 1',
            vscode.CodeActionKind.QuickFix
        );
        action.edit = new vscode.WorkspaceEdit();
        action.edit.insert(document.uri, new vscode.Position(0, 0), 'schema_version: 1\n');
        action.isPreferred = true;
        return action;
    }

    private createNameFix(document: vscode.TextDocument): vscode.CodeAction {
        const action = new vscode.CodeAction(
            'Add workflow name',
            vscode.CodeActionKind.QuickFix
        );
        action.edit = new vscode.WorkspaceEdit();
        action.command = {
            command: 'autoflow.addName',
            title: 'Add workflow name',
            arguments: [document.uri]
        };
        return action;
    }

    private createTasksFix(document: vscode.TextDocument): vscode.CodeAction {
        const action = new vscode.CodeAction(
            'Add tasks section',
            vscode.CodeActionKind.QuickFix
        );
        action.edit = new vscode.WorkspaceEdit();
        const lastLine = document.lineCount;
        const tasksTemplate = '\ntasks:\n  main:\n    steps:\n      - ';
        action.edit.insert(document.uri, new vscode.Position(lastLine, 0), tasksTemplate);
        action.isPreferred = true;
        return action;
    }

    private extractUnknownKeyword(message: string): string | null {
        const match = message.match(/Unknown keyword: "([^"]+)"/);
        return match ? match[1] : null;
    }

    private createKeywordSuggestion(
        document: vscode.TextDocument,
        range: vscode.Range,
        unknownKeyword: string
    ): vscode.CodeAction {
        const action = new vscode.CodeAction(
            `Search for "${unknownKeyword}" in documentation`,
            vscode.CodeActionKind.QuickFix
        );
        action.command = {
            command: 'autoflow.searchKeyword',
            title: 'Search keyword',
            arguments: [unknownKeyword]
        };
        return action;
    }
}
