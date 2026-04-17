import * as vscode from 'vscode';

export class AutoFlowStatusBar {
    private readonly statusItem: vscode.StatusBarItem;
    private lastRunStatus: 'success' | 'failed' | 'running' | 'idle' = 'idle';
    private lastRunTime?: Date;

    constructor() {
        this.statusItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
        this.statusItem.command = 'autoflow.showStatus';
        this.updateIdle();
        this.statusItem.show();
    }

    setRunning(): void {
        this.lastRunStatus = 'running';
        this.statusItem.text = '$(sync~spin) AutoFlow: Running...';
        this.statusItem.backgroundColor = undefined;
        this.statusItem.tooltip = 'Workflow is executing...';
    }

    setSuccess(): void {
        this.lastRunStatus = 'success';
        this.lastRunTime = new Date();
        this.statusItem.text = '$(check) AutoFlow';
        this.statusItem.backgroundColor = undefined;
        this.statusItem.tooltip = `Last run: Success (${this.formatTime(this.lastRunTime)})`;
        setTimeout(() => this.updateIdle(), 5000);
    }

    setFailed(error?: string): void {
        this.lastRunStatus = 'failed';
        this.lastRunTime = new Date();
        this.statusItem.text = '$(error) AutoFlow';
        this.statusItem.backgroundColor = new vscode.ThemeColor('statusBarItem.errorBackground');
        this.statusItem.tooltip = `Last run: Failed - ${error || 'Unknown error'} (${this.formatTime(this.lastRunTime)})`;
        setTimeout(() => this.updateIdle(), 10000);
    }

    private updateIdle(): void {
        if (this.lastRunStatus === 'running') return;
        this.lastRunStatus = 'idle';
        this.statusItem.text = '$(workflow) AutoFlow';
        this.statusItem.backgroundColor = undefined;
        this.statusItem.tooltip = this.lastRunTime
            ? `AutoFlow Ready\nLast run: ${this.formatTime(this.lastRunTime)}`
            : 'AutoFlow Ready - Click to run workflow';
    }

    private formatTime(date: Date): string {
        return date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
    }

    showQuickPick(): void {
        const options = [
            { label: '$(play) Run Workflow', action: 'run' },
            { label: '$(check) Validate Workflow', action: 'validate' },
            { label: '$(history) Show History', action: 'history' },
            { label: '$(list-unordered) List Keywords', action: 'keywords' },
            { label: '$(graph) Show Statistics', action: 'stats' },
        ];

        if (this.lastRunTime) {
            options.push({ 
                label: `$(clock) Last run: ${this.formatTime(this.lastRunTime)}`, 
                action: 'none' 
            });
        }

        vscode.window.showQuickPick(options, { placeHolder: 'AutoFlow Actions' }).then(selection => {
            if (!selection || selection.action === 'none') return;
            vscode.commands.executeCommand(`autoflow.${selection.action === 'keywords' ? 'listKeywords' : selection.action}`);
        });
    }

    dispose(): void {
        this.statusItem.dispose();
    }
}
