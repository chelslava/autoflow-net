using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AutoFlow.Runtime.Telemetry;

public sealed class TelemetryProvider
{
    private const string ActivitySourceName = "AutoFlow.Runtime";
    private const string MeterName = "AutoFlow.Runtime";

    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly Counter<long> _workflowCounter;
    private readonly Counter<long> _stepCounter;
    private readonly Histogram<double> _workflowDuration;
    private readonly Histogram<double> _stepDuration;
    private readonly bool _enabled;

    public TelemetryProvider(bool enabled = true)
    {
        _enabled = enabled;
        _activitySource = new ActivitySource(ActivitySourceName, "1.0.0");
        _meter = new Meter(MeterName, "1.0.0");

        _workflowCounter = _meter.CreateCounter<long>("autoflow_workflows_total", "count", "Total number of workflow executions");
        _stepCounter = _meter.CreateCounter<long>("autoflow_steps_total", "count", "Total number of step executions");
        _workflowDuration = _meter.CreateHistogram<double>("autoflow_workflow_duration_ms", "ms", "Workflow execution duration");
        _stepDuration = _meter.CreateHistogram<double>("autoflow_step_duration_ms", "ms", "Step execution duration");
    }

    public Activity? StartWorkflowSpan(string workflowName, string runId, IDictionary<string, object?>? tags = null)
    {
        if (!_enabled) return null;

        var activity = _activitySource.StartActivity($"workflow.{workflowName}", ActivityKind.Internal);
        if (activity is null) return null;

        activity.SetTag("workflow.name", workflowName);
        activity.SetTag("workflow.run_id", runId);

        if (tags is not null)
        {
            foreach (var (key, value) in tags)
            {
                activity.SetTag(key, value);
            }
        }

        return activity;
    }

    public Activity? StartStepSpan(string workflowName, string stepId, string keywordName, IDictionary<string, object?>? tags = null)
    {
        if (!_enabled) return null;

        var activity = _activitySource.StartActivity($"step.{keywordName}", ActivityKind.Internal);
        if (activity is null) return null;

        activity.SetTag("workflow.name", workflowName);
        activity.SetTag("step.id", stepId);
        activity.SetTag("step.keyword", keywordName);

        if (tags is not null)
        {
            foreach (var (key, value) in tags)
            {
                activity.SetTag(key, value);
            }
        }

        return activity;
    }

    public void RecordWorkflowStart(string workflowName)
    {
        if (!_enabled) return;
        _workflowCounter.Add(1, new KeyValuePair<string, object?>("workflow", workflowName), new KeyValuePair<string, object?>("status", "started"));
    }

    public void RecordWorkflowEnd(string workflowName, string status, double durationMs)
    {
        if (!_enabled) return;
        _workflowCounter.Add(1, new KeyValuePair<string, object?>("workflow", workflowName), new KeyValuePair<string, object?>("status", status));
        _workflowDuration.Record(durationMs, new KeyValuePair<string, object?>("workflow", workflowName), new KeyValuePair<string, object?>("status", status));
    }

    public void RecordStepEnd(string workflowName, string keywordName, string status, double durationMs)
    {
        if (!_enabled) return;
        _stepCounter.Add(1, new KeyValuePair<string, object?>("workflow", workflowName), new KeyValuePair<string, object?>("keyword", keywordName), new KeyValuePair<string, object?>("status", status));
        _stepDuration.Record(durationMs, new KeyValuePair<string, object?>("workflow", workflowName), new KeyValuePair<string, object?>("keyword", keywordName));
    }
}
