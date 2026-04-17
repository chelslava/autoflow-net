import * as vscode from 'vscode';
import { extractStepIds } from './keywords';

export class AutoFlowDefinitionProvider implements vscode.DefinitionProvider {
    provideDefinition(
        document: vscode.TextDocument,
        position: vscode.Position,
        token: vscode.CancellationToken
    ): vscode.ProviderResult<vscode.Definition | vscode.LocationLink[]> {
        const range = document.getWordRangeAtPosition(position, /\$\{steps\.([a-zA-Z_][a-zA-Z0-9_]*)/);
        if (!range) {
            return null;
        }

        const text = document.getText(range);
        const match = text.match(/\$\{steps\.([a-zA-Z_][a-zA-Z0-9_]*)/);
        if (!match) {
            return null;
        }

        const stepId = match[1];
        const textDoc = document.getText();
        const stepIds = extractStepIds(textDoc);

        if (!stepIds.has(stepId)) {
            return null;
        }

        const lines = textDoc.split('\n');
        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const idMatch = line.match(new RegExp(`^\\s*id:\\s*${stepId}\\s*$`));
            if (idMatch) {
                const idStart = line.indexOf(stepId);
                return new vscode.Location(
                    document.uri,
                    new vscode.Range(i, idStart, i, idStart + stepId.length)
                );
            }
        }

        return null;
    }
}
