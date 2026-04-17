import * as vscode from 'vscode';
import { KEYWORDS, getKeyword } from './keywords';

export class AutoFlowSignatureHelpProvider implements vscode.SignatureHelpProvider {
    provideSignatureHelp(
        document: vscode.TextDocument,
        position: vscode.Position,
        token: vscode.CancellationToken,
        context: vscode.SignatureHelpContext
    ): vscode.ProviderResult<vscode.SignatureHelp> {
        const line = document.lineAt(position).text;
        const textBeforeCursor = line.substring(0, position.character);

        const keyword = this.findKeywordInContext(document, position);
        if (!keyword) return null;

        const currentArg = this.getCurrentArgumentIndex(textBeforeCursor);
        const activeParameter = this.getActiveParameter(textBeforeCursor, currentArg);

        const signatureHelp = new vscode.SignatureHelp();
        
        const signature = new vscode.SignatureInformation(
            `${keyword.name}(${keyword.args.map(a => `${a.name}: ${a.type}`).join(', ')})`,
            new vscode.MarkdownString(keyword.description)
        );

        signature.parameters = keyword.args.map(arg => {
            const label = `${arg.name}: ${arg.type}`;
            const doc = new vscode.MarkdownString(
                `**${arg.name}**\n\n${arg.description}` +
                (arg.required ? '\n\n*Required*' : '') +
                (arg.default ? `\n\nDefault: \`${arg.default}\`` : '') +
                (arg.enum ? `\n\nOptions: ${arg.enum.map(e => `\`${e}\``).join(', ')}` : '')
            );
            return new vscode.ParameterInformation(label, doc);
        });

        signatureHelp.signatures = [signature];
        signatureHelp.activeSignature = 0;
        signatureHelp.activeParameter = activeParameter;

        return signatureHelp;
    }

    private findKeywordInContext(document: vscode.TextDocument, position: vscode.Position) {
        for (let i = position.line; i >= Math.max(0, position.line - 30); i--) {
            const line = document.lineAt(i).text;
            const match = line.match(/uses:\s*([a-zA-Z_.]+)/);
            if (match) {
                return getKeyword(match[1]);
            }
            if (line.trim().startsWith('- step:') || line.trim().startsWith('id:')) {
                continue;
            }
            if (i < position.line && line.trim().startsWith('- ')) {
                break;
            }
        }
        return null;
    }

    private getCurrentArgumentIndex(text: string): number {
        let depth = 0;
        let argIndex = 0;
        
        for (let i = 0; i < text.length; i++) {
            const char = text[i];
            if (char === '{' || char === '[') depth++;
            else if (char === '}' || char === ']') depth--;
            else if (char === ':' && depth === 0) {
                const before = text.substring(0, i).trim();
                const words = before.split(/\s+/);
                const lastWord = words[words.length - 1];
                if (lastWord && /^[a-zA-Z_][a-zA-Z0-9_]*$/.test(lastWord)) {
                    argIndex++;
                }
            }
        }
        
        return Math.max(0, argIndex - 1);
    }

    private getActiveParameter(text: string, argIndex: number): number {
        const lines = text.split('\n');
        let inWith = false;
        let paramIndex = 0;

        for (const line of lines) {
            const trimmed = line.trim();
            if (trimmed === 'with:') {
                inWith = true;
                continue;
            }
            if (inWith && trimmed.includes(':')) {
                const match = trimmed.match(/^([a-zA-Z_][a-zA-Z0-9_]*):/);
                if (match) {
                    if (text.includes(line) && text.indexOf(line) + line.indexOf(':') >= text.length - 1) {
                        return paramIndex;
                    }
                    paramIndex++;
                }
            }
        }

        return argIndex;
    }
}
