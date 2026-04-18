import { DebugAdapterDescriptorFactory, DebugAdapterDescriptor, DebugAdapterExecutable, DebugSession, DebugAdapterInlineImplementation } from 'vscode';
import { AutoFlowDebugSession } from './autoFlowDebugSession';

export class AutoFlowDebugAdapterDescriptorFactory implements DebugAdapterDescriptorFactory {
    createDebugAdapterDescriptor(
        session: DebugSession,
        _executable: DebugAdapterExecutable | undefined
    ): DebugAdapterDescriptor {
        return new DebugAdapterInlineImplementation(new AutoFlowDebugSession());
    }
}
