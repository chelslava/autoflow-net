import * as vscode from 'vscode';
import { getKeyword, KEYWORDS } from './keywords';

export class AutoFlowTreeDataProvider implements vscode.TreeDataProvider<TreeItem> {
    private _onDidChangeTreeData = new vscode.EventEmitter<TreeItem | undefined | null | void>();
    readonly onDidChangeTreeData = this._onDidChangeTreeData.event;

    private document: vscode.TextDocument | undefined;

    refresh(document?: vscode.TextDocument): void {
        this.document = document;
        this._onDidChangeTreeData.fire();
    }

    getTreeItem(element: TreeItem): vscode.TreeItem {
        return element;
    }

    getChildren(element?: TreeItem): Thenable<TreeItem[]> {
        if (!this.document) {
            const editor = vscode.window.activeTextEditor;
            if (editor && this.isAutoFlowDocument(editor.document)) {
                this.document = editor.document;
            } else {
                return Promise.resolve([]);
            }
        }

        const text = this.document.getText();
        const lines = text.split('\n');

        if (!element) {
            return Promise.resolve(this.getRootItems(text, lines));
        }

        if (element.contextValue === 'task') {
            return Promise.resolve(this.getTaskChildren(element, text, lines));
        }

        if (element.contextValue === 'step') {
            return Promise.resolve(this.getStepChildren(element, text, lines));
        }

        return Promise.resolve([]);
    }

    private isAutoFlowDocument(doc: vscode.TextDocument): boolean {
        return doc.languageId === 'autoflow' || 
               doc.fileName.endsWith('.yaml') || 
               doc.fileName.endsWith('.yml');
    }

    private getRootItems(text: string, lines: string[]): TreeItem[] {
        const items: TreeItem[] = [];

        const nameMatch = text.match(/name:\s*(.+)/);
        if (nameMatch) {
            const item = new TreeItem(`Workflow: ${nameMatch[1].trim()}`, vscode.TreeItemCollapsibleState.None);
            item.iconPath = new vscode.ThemeIcon('file-code');
            items.push(item);
        }

        const varsMatch = text.match(/variables:/);
        if (varsMatch) {
            const vars = this.extractVariables(text);
            const item = new TreeItem(`Variables (${vars.size})`, vscode.TreeItemCollapsibleState.Collapsed);
            item.iconPath = new vscode.ThemeIcon('symbol-variable');
            item.contextValue = 'variables';
            item.children = vars;
            items.push(item);
        }

        const tasksMatch = text.match(/tasks:/);
        if (tasksMatch) {
            const tasks = this.extractTasks(text, lines);
            for (const task of tasks) {
                const item = new TreeItem(task.name, vscode.TreeItemCollapsibleState.Collapsed);
                item.iconPath = new vscode.ThemeIcon('symbol-function');
                item.contextValue = 'task';
                item.line = task.line;
                item.command = {
                    command: 'autoflow.gotoLine',
                    title: 'Go to Task',
                    arguments: [task.line]
                };
                items.push(item);
            }
        }

        return items;
    }

    private getTaskChildren(element: TreeItem, text: string, lines: string[]): TreeItem[] {
        const items: TreeItem[] = [];
        const steps = this.extractStepsForTask(element.line || 0, lines);

        for (const step of steps) {
            const item = new TreeItem(step.id || step.keyword || 'step', vscode.TreeItemCollapsibleState.Collapsed);
            item.iconPath = new vscode.ThemeIcon(step.keyword?.includes('browser') ? 'browser' : 
                                                  step.keyword?.includes('http') ? 'globe' : 
                                                  step.keyword?.includes('file') ? 'file' : 'symbol-event');
            item.contextValue = 'step';
            item.line = step.line;
            item.keyword = step.keyword;
            item.stepId = step.id;
            item.command = {
                command: 'autoflow.gotoLine',
                title: 'Go to Step',
                arguments: [step.line]
            };
            items.push(item);
        }

        return items;
    }

    private getStepChildren(element: TreeItem, text: string, lines: string[]): TreeItem[] {
        const items: TreeItem[] = [];

        if (element.keyword) {
            const keyword = getKeyword(element.keyword);
            if (keyword) {
                const argsItem = new TreeItem(`Arguments`, vscode.TreeItemCollapsibleState.None);
                argsItem.iconPath = new vscode.ThemeIcon('symbol-property');
                argsItem.description = keyword.args.length.toString();
                items.push(argsItem);

                if (keyword.outputs && keyword.outputs.length > 0) {
                    const outputsItem = new TreeItem(`Outputs`, vscode.TreeItemCollapsibleState.None);
                    outputsItem.iconPath = new vscode.ThemeIcon('symbol-value');
                    outputsItem.description = keyword.outputs.join(', ');
                    items.push(outputsItem);
                }
            }
        }

        return items;
    }

    private extractVariables(text: string): Map<string, string> {
        const vars = new Map<string, string>();
        const inVars = text.match(/variables:\s*\n((?:\s+[a-zA-Z_][a-zA-Z0-9_]*:.*\n?)*)/);
        if (inVars) {
            const varPattern = /^\s+([a-zA-Z_][a-zA-Z0-9_]*):\s*(.*)$/gm;
            let match;
            while ((match = varPattern.exec(inVars[1])) !== null) {
                vars.set(match[1], match[2].trim());
            }
        }
        return vars;
    }

    private extractTasks(text: string, lines: string[]): { name: string; line: number }[] {
        const tasks: { name: string; line: number }[] = [];
        const taskPattern = /^(\s{2})([a-zA-Z_][a-zA-Z0-9_]*):\s*$/;
        let inTasks = false;

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            if (line.trim() === 'tasks:') {
                inTasks = true;
                continue;
            }
            if (inTasks) {
                const match = line.match(taskPattern);
                if (match) {
                    tasks.push({ name: match[2], line: i });
                } else if (line.trim().length > 0 && !line.startsWith('  ') && !line.startsWith('\t')) {
                    break;
                }
            }
        }

        return tasks;
    }

    private extractStepsForTask(taskLine: number, lines: string[]): { id?: string; keyword?: string; line: number }[] {
        const steps: { id?: string; keyword?: string; line: number }[] = [];
        const stepIdPattern = /^\s*id:\s*([a-zA-Z_][a-zA-Z0-9_]*)/;
        const usesPattern = /uses:\s*([a-zA-Z_.]+)/;
        
        let inSteps = false;
        let stepDepth = 0;

        for (let i = taskLine + 1; i < lines.length; i++) {
            const line = lines[i];
            const trimmed = line.trim();

            if (trimmed === 'steps:') {
                inSteps = true;
                stepDepth = line.search(/\S/);
                continue;
            }

            if (trimmed.startsWith('on_error:') || trimmed.startsWith('finally:')) {
                break;
            }

            if (inSteps) {
                const currentDepth = line.search(/\S/);
                if (currentDepth <= stepDepth && trimmed.length > 0 && !trimmed.startsWith('id:') && 
                    !trimmed.startsWith('uses:') && !trimmed.startsWith('with:') && 
                    !trimmed.startsWith('save_as:') && !trimmed.startsWith('-')) {
                    break;
                }

                if (trimmed.startsWith('- step:') || trimmed.startsWith('- parallel:') || 
                    trimmed.startsWith('- for_each:') || trimmed.startsWith('- if:')) {
                    const step: { id?: string; keyword?: string; line: number } = { line: i };
                    
                    for (let j = i; j < Math.min(i + 10, lines.length); j++) {
                        const idMatch = lines[j].match(stepIdPattern);
                        if (idMatch) step.id = idMatch[1];
                        
                        const usesMatch = lines[j].match(usesPattern);
                        if (usesMatch) step.keyword = usesMatch[1];
                    }
                    
                    steps.push(step);
                }
            }
        }

        return steps;
    }
}

class TreeItem extends vscode.TreeItem {
    children?: Map<string, string>;
    line?: number;
    keyword?: string;
    stepId?: string;

    constructor(label: string, collapsibleState: vscode.TreeItemCollapsibleState) {
        super(label, collapsibleState);
    }
}
