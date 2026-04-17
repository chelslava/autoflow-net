import * as vscode from 'vscode';
import { KEYWORDS, TOP_LEVEL_KEYWORDS, TASK_KEYWORDS, STEP_KEYWORDS, STEP_PROPS, RETRY_PROPS, getKeyword, extractWorkflowVariables, extractStepIds } from './keywords';

export class AutoFlowCompletionProvider implements vscode.CompletionItemProvider {
    provideCompletionItems(
        document: vscode.TextDocument,
        position: vscode.Position,
        token: vscode.CancellationToken,
        context: vscode.CompletionContext
    ): vscode.ProviderResult<vscode.CompletionItem[] | vscode.CompletionList> {
        const line = document.lineAt(position).text;
        const textBeforeCursor = line.substring(0, position.character);
        const items: vscode.CompletionItem[] = [];

        if (this.isAfterUses(textBeforeCursor)) {
            return this.getKeywordCompletions();
        }

        if (this.isAfterWith(textBeforeCursor)) {
            const keyword = this.getCurrentKeyword(document, position);
            if (keyword) {
                return this.getArgumentCompletions(keyword);
            }
        }

        if (this.isAfterSaveAs(textBeforeCursor)) {
            return this.getSaveAsCompletions(document, position);
        }

        if (this.isInVariableReference(textBeforeCursor)) {
            return this.getVariableReferenceCompletions(document, textBeforeCursor);
        }

        if (this.isAfterEnvOrSecret(textBeforeCursor)) {
            return this.getEnvSecretCompletions(textBeforeCursor);
        }

        if (this.isAfterStepsRef(textBeforeCursor)) {
            return this.getStepIdCompletions(document);
        }

        if (this.isAfterStepsOutputs(textBeforeCursor)) {
            return this.getOutputCompletions(document, textBeforeCursor);
        }

        if (this.isInVariablesSection(document, position)) {
            items.push(...this.getVariableSnippet());
        }

        if (this.isTopLevel(document, position)) {
            items.push(...this.getTopLevelCompletions());
        }

        if (this.isInTasksSection(document, position)) {
            items.push(...this.getTaskCompletions());
        }

        if (this.isInStepsArray(document, position)) {
            items.push(...this.getStepCompletions());
        }

        if (this.isAfterStepProp(textBeforeCursor)) {
            items.push(...this.getStepPropCompletions());
        }

        if (this.isInRetryBlock(document, position)) {
            items.push(...this.getRetryCompletions());
        }

        return items;
    }

    private isAfterUses(text: string): boolean {
        return /uses:\s*$/.test(text) || /uses:\s*["']?\s*$/.test(text);
    }

    private isAfterWith(text: string): boolean {
        return /with:\s*$/.test(text);
    }

    private isAfterSaveAs(text: string): boolean {
        return /save_as:\s*$/.test(text);
    }

    private isAfterStepProp(text: string): boolean {
        const indent = text.match(/^(\s*)/)?.[1] ?? '';
        return indent.length > 0 && /:\s*$/.test(text);
    }

    private isInVariableReference(text: string): boolean {
        return /\$\{[^}]*$/.test(text);
    }

    private isAfterEnvOrSecret(text: string): boolean {
        return /\$\{(env|secret):[^}]*$/.test(text);
    }

    private isAfterStepsRef(text: string): boolean {
        return /\$\{steps\.[^}]*$/.test(text) && !text.includes('.outputs');
    }

    private isAfterStepsOutputs(text: string): boolean {
        return /\$\{steps\.[^.}]+\.outputs\.[^}]*$/.test(text);
    }

    private isTopLevel(document: vscode.TextDocument, position: vscode.Position): boolean {
        for (let i = position.line - 1; i >= 0; i--) {
            const line = document.lineAt(i).text;
            if (line.trim().length === 0) continue;
            const indent = line.match(/^(\s*)/)?.[1] ?? '';
            if (indent.length === 0 && !line.startsWith('#')) {
                return false;
            }
        }
        return true;
    }

    private isInVariablesSection(document: vscode.TextDocument, position: vscode.Position): boolean {
        return this.isInSection(document, position, 'variables:');
    }

    private isInTasksSection(document: vscode.TextDocument, position: vscode.Position): boolean {
        return this.isInSection(document, position, 'tasks:');
    }

    private isInStepsArray(document: vscode.TextDocument, position: vscode.Position): boolean {
        return this.isInSection(document, position, 'steps:');
    }

    private isInRetryBlock(document: vscode.TextDocument, position: vscode.Position): boolean {
        return this.isInSection(document, position, 'retry:');
    }

    private isInSection(document: vscode.TextDocument, position: vscode.Position, sectionStart: string): boolean {
        for (let i = position.line - 1; i >= 0; i--) {
            const line = document.lineAt(i).text;
            if (line.trim().startsWith(sectionStart)) {
                return true;
            }
            if (line.trim().length > 0 && !line.startsWith(' ') && !line.startsWith('\t')) {
                return false;
            }
        }
        return false;
    }

    private getCurrentKeyword(document: vscode.TextDocument, position: vscode.Position) {
        for (let i = position.line - 1; i >= Math.max(0, position.line - 20); i--) {
            const line = document.lineAt(i).text;
            const match = line.match(/uses:\s*([a-zA-Z_.]+)/);
            if (match) {
                return getKeyword(match[1]);
            }
        }
        return null;
    }

    private getKeywordCompletions(): vscode.CompletionItem[] {
        return KEYWORDS.map(k => {
            const item = new vscode.CompletionItem(k.name, vscode.CompletionItemKind.Function);
            item.documentation = new vscode.MarkdownString(`**${k.category}**\n\n${k.description}`);
            item.detail = k.category;
            item.sortText = k.category === 'HTTP' ? '0' : k.category === 'Browser' ? '1' : '2';
            return item;
        });
    }

    private getArgumentCompletions(keyword: ReturnType<typeof getKeyword>): vscode.CompletionItem[] {
        if (!keyword) return [];
        
        return keyword.args.map(arg => {
            const item = new vscode.CompletionItem(arg.name, vscode.CompletionItemKind.Property);
            item.documentation = new vscode.MarkdownString(
                `${arg.description}\n\n**Type:** ${arg.type}${arg.required ? ' *(required)*' : ''}`
            );
            
            if (arg.enum) {
                item.insertText = new vscode.SnippetString(`${arg.name}: \${1|${arg.enum.join(',')}|}`);
            } else if (arg.default) {
                item.insertText = new vscode.SnippetString(`${arg.name}: \${1:${arg.default}}`);
            } else {
                item.insertText = new vscode.SnippetString(`${arg.name}: \${1:${this.getDefaultValueForType(arg.type)}}`);
            }
            
            item.sortText = arg.required ? '0' : '1';
            return item;
        });
    }

    private getDefaultValueForType(type: string): string {
        switch (type) {
            case 'string': return '""';
            case 'number': return '0';
            case 'boolean': return 'false';
            case 'object': return '{}';
            default: return '';
        }
    }

    private getSaveAsCompletions(document: vscode.TextDocument, position: vscode.Position): vscode.CompletionItem[] {
        const keyword = this.getCurrentKeyword(document, position);
        if (!keyword?.outputs) {
            return [];
        }
        return keyword.outputs.map(output => {
            const item = new vscode.CompletionItem(output, vscode.CompletionItemKind.Variable);
            item.insertText = new vscode.SnippetString(`${output}: \${1:${output}}`);
            return item;
        });
    }

    private getVariableReferenceCompletions(document: vscode.TextDocument, text: string): vscode.CompletionItem[] {
        const docText = document.getText();
        const workflowVars = extractWorkflowVariables(docText);
        const items: vscode.CompletionItem[] = [];

        const varPrefix = new vscode.CompletionItem('var', vscode.CompletionItemKind.Variable);
        varPrefix.documentation = new vscode.MarkdownString('Simple variable reference');
        items.push(varPrefix);

        const envPrefix = new vscode.CompletionItem('env:', vscode.CompletionItemKind.Variable);
        envPrefix.documentation = new vscode.MarkdownString('Environment variable');
        envPrefix.insertText = new vscode.SnippetString('env:\${1:VAR_NAME}');
        items.push(envPrefix);

        const secretPrefix = new vscode.CompletionItem('secret:', vscode.CompletionItemKind.Variable);
        secretPrefix.documentation = new vscode.MarkdownString('Secret value');
        secretPrefix.insertText = new vscode.SnippetString('secret:\${1:SECRET_NAME}');
        items.push(secretPrefix);

        const stepsPrefix = new vscode.CompletionItem('steps.', vscode.CompletionItemKind.Variable);
        stepsPrefix.documentation = new vscode.MarkdownString('Step output reference');
        stepsPrefix.insertText = new vscode.SnippetString('steps.\${1:step_id}.outputs.\${2:output}');
        items.push(stepsPrefix);

        for (const [varName, varValue] of workflowVars) {
            const item = new vscode.CompletionItem(varName, vscode.CompletionItemKind.Variable);
            item.detail = `= ${varValue.substring(0, 30)}${varValue.length > 30 ? '...' : ''}`;
            item.insertText = varName;
            items.push(item);
        }

        return items;
    }

    private getEnvSecretCompletions(text: string): vscode.CompletionItem[] {
        const items: vscode.CompletionItem[] = [];
        
        const commonEnvs = ['HOME', 'PATH', 'USER', 'TEMP', 'TMPDIR', 'PWD'];
        for (const env of commonEnvs) {
            const item = new vscode.CompletionItem(env, vscode.CompletionItemKind.Variable);
            item.insertText = env + '}';
            items.push(item);
        }
        
        return items;
    }

    private getStepIdCompletions(document: vscode.TextDocument): vscode.CompletionItem[] {
        const docText = document.getText();
        const stepIds = extractStepIds(docText);
        const items: vscode.CompletionItem[] = [];

        for (const stepId of stepIds) {
            const item = new vscode.CompletionItem(stepId, vscode.CompletionItemKind.Reference);
            item.insertText = stepId + '.outputs.';
            items.push(item);
        }

        return items;
    }

    private getOutputCompletions(document: vscode.TextDocument, text: string): vscode.CompletionItem[] {
        const match = text.match(/\$\{steps\.([^.}]+)\.outputs\.([^}]*)$/);
        if (!match) return [];

        const stepId = match[1];
        const docText = document.getText();
        
        const lines = docText.split('\n');
        let foundKeyword: ReturnType<typeof getKeyword> = null;
        
        for (let i = 0; i < lines.length; i++) {
            if (lines[i].includes(`id: ${stepId}`) || lines[i].includes(`id:${stepId}`)) {
                for (let j = i; j < Math.min(i + 10, lines.length); j++) {
                    const usesMatch = lines[j].match(/uses:\s*([a-zA-Z_.]+)/);
                    if (usesMatch) {
                        foundKeyword = getKeyword(usesMatch[1]);
                        break;
                    }
                }
                break;
            }
        }

        if (!foundKeyword?.outputs) return [];
        
        return foundKeyword.outputs.map(output => {
            const item = new vscode.CompletionItem(output, vscode.CompletionItemKind.Property);
            item.insertText = output + '}';
            return item;
        });
    }

    private getVariableSnippet(): vscode.CompletionItem[] {
        const item = new vscode.CompletionItem('${', vscode.CompletionItemKind.Snippet);
        item.insertText = new vscode.SnippetString('\${${1|var,env:,secret:,steps.|}}');
        item.documentation = new vscode.MarkdownString(
            '**Variable Syntax**\n\n' +
            '- `${var}` - Simple variable\n' +
            '- `${env:NAME}` - Environment variable\n' +
            '- `${secret:NAME}` - Secret value\n' +
            '- `${steps.id.outputs.key}` - Step output'
        );
        return [item];
    }

    private getTopLevelCompletions(): vscode.CompletionItem[] {
        return TOP_LEVEL_KEYWORDS.map(k => {
            const item = new vscode.CompletionItem(k.name, vscode.CompletionItemKind.Keyword);
            item.documentation = new vscode.MarkdownString(k.description);
            return item;
        });
    }

    private getTaskCompletions(): vscode.CompletionItem[] {
        return TASK_KEYWORDS.map(k => {
            const item = new vscode.CompletionItem(k.name, vscode.CompletionItemKind.Keyword);
            item.documentation = new vscode.MarkdownString(k.description);
            return item;
        });
    }

    private getStepCompletions(): vscode.CompletionItem[] {
        return STEP_KEYWORDS.map(k => {
            const item = new vscode.CompletionItem(k.name, vscode.CompletionItemKind.Keyword);
            item.documentation = new vscode.MarkdownString(k.description);
            return item;
        });
    }

    private getStepPropCompletions(): vscode.CompletionItem[] {
        return STEP_PROPS.map(k => {
            const item = new vscode.CompletionItem(k.name, vscode.CompletionItemKind.Property);
            item.documentation = new vscode.MarkdownString(k.description);
            return item;
        });
    }

    private getRetryCompletions(): vscode.CompletionItem[] {
        return RETRY_PROPS.map(k => {
            const item = new vscode.CompletionItem(k.name, vscode.CompletionItemKind.Property);
            item.documentation = new vscode.MarkdownString(k.description);
            if ('enum' in k && k.enum) {
                item.insertText = new vscode.SnippetString(`${k.name}: \${1|${k.enum.join(',')}|}`);
            }
            return item;
        });
    }
}
