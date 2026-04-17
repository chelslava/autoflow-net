import * as vscode from 'vscode';

export class AutoFlowFoldingProvider implements vscode.FoldingRangeProvider {
    provideFoldingRanges(
        document: vscode.TextDocument,
        context: vscode.FoldingContext,
        token: vscode.CancellationToken
    ): vscode.ProviderResult<vscode.FoldingRange[]> {
        const ranges: vscode.FoldingRange[] = [];
        const text = document.getText();
        const lines = text.split('\n');

        const blockKeywords = ['tasks:', 'variables:', 'steps:', 'on_error:', 'finally:', 'retry:', 'with:', 'save_as:'];
        const stack: { line: number; indent: number; keyword?: string }[] = [];

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const trimmed = line.trim();
            
            if (trimmed.length === 0 || trimmed.startsWith('#')) {
                continue;
            }

            const indent = line.search(/\S/);
            
            while (stack.length > 0 && stack[stack.length - 1].indent >= indent) {
                const start = stack.pop()!;
                if (i - start.line > 1) {
                    ranges.push(new vscode.FoldingRange(start.line, i - 1));
                }
            }

            for (const keyword of blockKeywords) {
                if (trimmed.startsWith(keyword)) {
                    stack.push({ line: i, indent, keyword });
                    break;
                }
            }

            if (trimmed.startsWith('- step:') || trimmed.startsWith('- parallel:') || 
                trimmed.startsWith('- for_each:') || trimmed.startsWith('- if:') ||
                trimmed.startsWith('- call:') || trimmed.startsWith('- group:')) {
                stack.push({ line: i, indent });
            }
        }

        while (stack.length > 0) {
            const start = stack.pop()!;
            if (lines.length - start.line > 1) {
                ranges.push(new vscode.FoldingRange(start.line, lines.length - 1));
            }
        }

        return ranges;
    }
}
