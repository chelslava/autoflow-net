import {
    DebugSession,
    InitializedEvent,
    StoppedEvent,
    TerminatedEvent,
    OutputEvent,
    Thread,
    StackFrame,
    Scope,
    Source,
    Handles,
    Breakpoint,
    BreakpointEvent
} from '@vscode/debugadapter';
import { DebugProtocol } from '@vscode/debugprotocol';
import * as fs from 'fs';
import * as path from 'path';

interface LaunchRequestArguments extends DebugProtocol.LaunchRequestArguments {
    program: string;
    stopOnEntry?: boolean;
}

export class AutoFlowDebugSession extends DebugSession {
    private static THREAD_ID = 1;
    private _variableHandles = new Handles<'locals' | 'globals'>();
    private _currentLine = 0;
    private _sourceFile: string | undefined;
    private _sourceLines: string[] = [];
    private _breakpoints = new Map<string, number[]>();
    private _stopped = false;

    public constructor() {
        super();
    }

    protected override initializeRequest(
        response: DebugProtocol.InitializeResponse,
        _args: DebugProtocol.InitializeRequestArguments
    ): void {
        response.body = response.body || {};
        
        response.body.supportsConfigurationDoneRequest = true;
        response.body.supportsStepBack = false;
        response.body.supportsRestartFrame = false;
        response.body.supportsGotoTargetsRequest = false;
        response.body.supportsStepInTargetsRequest = false;
        response.body.supportsCompletionsRequest = false;
        response.body.supportsModulesRequest = false;
        response.body.supportsExceptionOptions = false;
        response.body.supportsValueFormattingOptions = false;
        response.body.supportsExceptionInfoRequest = false;
        response.body.supportTerminateDebuggee = true;
        response.body.supportsDelayedStackTraceLoading = false;
        response.body.supportsHitConditionalBreakpoints = false;
        response.body.supportsConditionalBreakpoints = false;
        response.body.supportsFunctionBreakpoints = false;
        response.body.supportsEvaluateForHovers = false;
        response.body.supportsSetVariable = false;
        response.body.supportsReadMemoryRequest = false;
        response.body.supportsDisassembleRequest = false;
        response.body.supportsCancelRequest = false;
        
        this.sendResponse(response);
        this.sendEvent(new InitializedEvent());
    }

    protected override launchRequest(
        response: DebugProtocol.LaunchResponse,
        args: LaunchRequestArguments
    ): void {
        if (args.program) {
            this._sourceFile = args.program;
            
            if (fs.existsSync(args.program)) {
                this._sourceLines = fs.readFileSync(args.program, 'utf-8').split('\n');
            }
        }

        this.sendResponse(response);
        
        this.sendEvent(new OutputEvent(`Starting workflow: ${args.program}\n`));
        
        if (args.stopOnEntry) {
            this._stopped = true;
            this.sendEvent(new StoppedEvent('entry', AutoFlowDebugSession.THREAD_ID));
        } else {
            this.continue();
        }
    }

    protected override threadsRequest(response: DebugProtocol.ThreadsResponse): void {
        response.body = {
            threads: [new Thread(AutoFlowDebugSession.THREAD_ID, 'Main Thread')]
        };
        this.sendResponse(response);
    }

    protected override stackTraceRequest(
        response: DebugProtocol.StackTraceResponse,
        _args: DebugProtocol.StackTraceArguments
    ): void {
        const frames: StackFrame[] = [];
        
        if (this._sourceFile) {
            const name = path.basename(this._sourceFile);
            const source = new Source(name, this._sourceFile);
            
            frames.push(new StackFrame(
                0,
                'Workflow Execution',
                source,
                this._currentLine + 1,
                0
            ));
        }

        response.body = { stackFrames: frames };
        this.sendResponse(response);
    }

    protected override scopesRequest(
        response: DebugProtocol.ScopesResponse,
        _args: DebugProtocol.ScopesArguments
    ): void {
        const scopes: Scope[] = [
            new Scope('Workflow Variables', this._variableHandles.create('locals'), false),
            new Scope('Environment', this._variableHandles.create('globals'), true)
        ];

        response.body = { scopes };
        this.sendResponse(response);
    }

    protected override variablesRequest(
        response: DebugProtocol.VariablesResponse,
        args: DebugProtocol.VariablesArguments
    ): void {
        const variables: DebugProtocol.Variable[] = [];

        const handle = this._variableHandles.get(args.variablesReference);
        if (handle === 'locals') {
            variables.push({
                name: 'step_id',
                value: '"current_step"',
                variablesReference: 0
            });
            variables.push({
                name: 'status',
                value: '"running"',
                variablesReference: 0
            });
        }

        response.body = { variables };
        this.sendResponse(response);
    }

    protected override continueRequest(
        response: DebugProtocol.ContinueResponse,
        _args: DebugProtocol.ContinueArguments
    ): void {
        this.continue();
        response.body = { allThreadsContinued: true };
        this.sendResponse(response);
    }

    protected override nextRequest(
        response: DebugProtocol.NextResponse,
        _args: DebugProtocol.NextArguments
    ): void {
        this.step();
        this.sendResponse(response);
    }

    protected override stepInRequest(
        response: DebugProtocol.StepInResponse,
        _args: DebugProtocol.StepInArguments
    ): void {
        this.step();
        this.sendResponse(response);
    }

    protected override stepOutRequest(
        response: DebugProtocol.StepOutResponse,
        _args: DebugProtocol.StepOutArguments
    ): void {
        this.step();
        this.sendResponse(response);
    }

    protected override disconnectRequest(
        response: DebugProtocol.DisconnectResponse,
        _args: DebugProtocol.DisconnectArguments
    ): void {
        this.sendResponse(response);
    }

    protected override setBreakPointsRequest(
        response: DebugProtocol.SetBreakpointsResponse,
        args: DebugProtocol.SetBreakpointsArguments
    ): void {
        const path = args.source.path;
        const clientLines = args.breakpoints?.map(bp => bp.line) || [];

        if (path) {
            this._breakpoints.set(path, clientLines);
        }

        const breakpoints = clientLines.map(line => {
            return new Breakpoint(true, line);
        });

        response.body = { breakpoints };
        this.sendResponse(response);
    }

    private continue(): void {
        if (this._sourceLines.length === 0) {
            this.sendEvent(new TerminatedEvent());
            return;
        }

        const interval = setInterval(() => {
            if (this._stopped) {
                clearInterval(interval);
                return;
            }

            const line = this._sourceLines[this._currentLine];
            if (line) {
                this.sendEvent(new OutputEvent(`${line}\n`));
            }

            if (this.checkBreakpoint()) {
                this._stopped = true;
                this.sendEvent(new StoppedEvent('breakpoint', AutoFlowDebugSession.THREAD_ID));
                clearInterval(interval);
                return;
            }

            this._currentLine++;

            if (this._currentLine >= this._sourceLines.length) {
                this.sendEvent(new OutputEvent('Workflow completed.\n'));
                this.sendEvent(new TerminatedEvent());
                clearInterval(interval);
            }
        }, 100);
    }

    private step(): void {
        this._currentLine++;
        
        if (this._currentLine >= this._sourceLines.length) {
            this.sendEvent(new TerminatedEvent());
        } else {
            this.sendEvent(new StoppedEvent('step', AutoFlowDebugSession.THREAD_ID));
        }
    }

    private checkBreakpoint(): boolean {
        if (!this._sourceFile) return false;
        
        const breakpoints = this._breakpoints.get(this._sourceFile);
        if (!breakpoints) return false;
        
        return breakpoints.includes(this._currentLine + 1);
    }
}
