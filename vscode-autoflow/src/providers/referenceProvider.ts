import * as vscode from 'vscode';

export class AutoFlowReferenceProvider implements vscode.ReferenceProvider {
    provideReferences(
        document: vscode.TextDocument,
        position: vscode.Position,
        context: vscode.ReferenceContext,
        token: vscode.CancellationToken
    ): vscode.ProviderResult<vscode.Location[]> {
        const range = document.getWordRangeAtPosition(position);
        if (!range) {
            return [];
        }

        const word = document.getText(range);
        const text = document.getText();
        const locations: vscode.Location[] = [];

        const varPattern = new RegExp(`\\$\\{${word}\\}`, 'g');
        let match;
        while ((match = varPattern.exec(text)) !== null) {
            const pos = document.positionAt(match.index);
            locations.push(new vscode.Location(document.uri, pos));
        }

        const defPattern = new RegExp(`^\\s*${word}:`, 'gm');
        while ((match = defPattern.exec(text)) !== null) {
            const pos = document.positionAt(match.index);
            if (!context.includeDeclaration && pos.line === position.line) {
                continue;
            }
            locations.push(new vscode.Location(document.uri, pos));
        }

        const stepIdPattern = new RegExp(`id:\\s*${word}\\s*$`, 'gm');
        while ((match = stepIdPattern.exec(text)) !== null) {
            const pos = document.positionAt(match.index);
            const idPos = text.indexOf(word, match.index);
            locations.push(new vscode.Location(
                document.uri,
                new vscode.Range(document.positionAt(idPos), document.positionAt(idPos + word.length))
            ));
        }

        const stepsRefPattern = new RegExp(`\\$\\{steps\\.${word}\\.`, 'g');
        while ((match = stepsRefPattern.exec(text)) !== null) {
            const pos = document.positionAt(match.index + 8);
            locations.push(new vscode.Location(document.uri, pos));
        }

        return locations;
    }
}
