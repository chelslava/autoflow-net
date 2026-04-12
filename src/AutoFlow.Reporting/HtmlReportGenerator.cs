// =============================================================================
// HtmlReportGenerator.cs — генератор HTML отчётов с маскированием секретов.
//
// Создаёт интерактивный HTML отчёт с деревом выполнения, статусами шагов,
// логами и длительностью. Поддерживает маскирование секретных значений.
// =============================================================================

using System.Text;
using AutoFlow.Abstractions;

namespace AutoFlow.Reporting;

public sealed class HtmlReportGenerator
{
    private readonly SecretMasker? _secretMasker;

    public HtmlReportGenerator() : this(null)
    {
    }

    public HtmlReportGenerator(SecretMasker? secretMasker)
    {
        _secretMasker = secretMasker;
    }

    public string Generate(RunResult runResult)
    {
        var html = new StringBuilder();
        html.Append("<!DOCTYPE html>");
        html.Append("<html lang=\"ru\">");
        html.Append("<head>");
        html.Append("<meta charset=\"UTF-8\">");
        html.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.Append($"<title>AutoFlow Report — {EscapeHtml(runResult.WorkflowName)}</title>");
        html.Append(GetStyles());
        html.Append("</head>");
        html.Append("<body>");
        html.Append("<div class=\"container\">");
        
        html.Append(GenerateHeader(runResult));
        html.Append(GenerateSummary(runResult));
        html.Append(GenerateStepsTree(runResult));
        
        html.Append("</div>");
        html.Append(GetScripts());
        html.Append("</body>");
        html.Append("</html>");
        
        return html.ToString();
    }

    private string GenerateHeader(RunResult runResult)
    {
        var statusClass = runResult.Status == ExecutionStatus.Passed ? "status-passed" : "status-failed";
        var statusIcon = runResult.Status == ExecutionStatus.Passed ? "✓" : "✗";
        
        return $@"
<header class=""header"">
    <h1 class=""workflow-name"">{EscapeHtml(runResult.WorkflowName)}</h1>
    <div class=""workflow-meta"">
        <span class=""status-badge {statusClass}"">{statusIcon} {runResult.Status}</span>
        <span class=""duration"">{FormatDuration(runResult.Duration)}</span>
        <span class=""timestamp"">{runResult.StartedAtUtc:yyyy-MM-dd HH:mm:ss} UTC</span>
    </div>
</header>";
    }

    private string GenerateSummary(RunResult runResult)
    {
        var passed = runResult.Steps.Count(s => s.Status == ExecutionStatus.Passed);
        var failed = runResult.Steps.Count(s => s.Status == ExecutionStatus.Failed);
        var skipped = runResult.Steps.Count(s => s.Status == ExecutionStatus.Skipped);
        
        return $@"
<section class=""summary"">
    <div class=""summary-card"">
        <div class=""summary-value"">{runResult.Steps.Count}</div>
        <div class=""summary-label"">Всего шагов</div>
    </div>
    <div class=""summary-card summary-card-passed"">
        <div class=""summary-value"">{passed}</div>
        <div class=""summary-label"">Пройдено</div>
    </div>
    <div class=""summary-card summary-card-failed"">
        <div class=""summary-value"">{failed}</div>
        <div class=""summary-label"">С ошибкой</div>
    </div>
    <div class=""summary-card summary-card-skipped"">
        <div class=""summary-value"">{skipped}</div>
        <div class=""summary-label"">Пропущено</div>
    </div>
</section>";
    }

    private string GenerateStepsTree(RunResult runResult)
    {
        var html = new StringBuilder();
        html.Append("<section class=\"steps-section\">");
        html.Append("<h2 class=\"section-title\">Дерево выполнения</h2>");
        html.Append("<div class=\"steps-tree\">");
        
        foreach (var step in runResult.Steps)
        {
            html.Append(GenerateStepNode(step));
        }
        
        html.Append("</div>");
        html.Append("</section>");
        
        return html.ToString();
    }

    private string GenerateStepNode(StepExecutionResult step)
    {
        var statusClass = step.Status.ToString().ToLowerInvariant();
        var statusIcon = step.Status switch
        {
            ExecutionStatus.Passed => "✓",
            ExecutionStatus.Failed => "✗",
            ExecutionStatus.Skipped => "○",
            _ => "?"
        };
        
        var html = new StringBuilder();
        html.Append($@"<div class=""step-node {statusClass}"">");
        
        html.Append($@"<div class=""step-header"" onclick=""toggleStep(this)"">");
        html.Append($@"<span class=""step-toggle"">▶</span>");
        html.Append($@"<span class=""step-status"">{statusIcon}</span>");
        html.Append($@"<span class=""step-id"">{EscapeHtml(step.StepId)}</span>");
        html.Append($@"<span class=""step-keyword"">{EscapeHtml(step.KeywordName)}</span>");
        html.Append($@"<span class=""step-duration"">{FormatDuration(step.Duration)}</span>");
        html.Append("</div>");
        
        html.Append("<div class=\"step-details\" style=\"display: none;\">");
        
        html.Append("<div class=\"detail-row\">");
        html.Append("<span class=\"detail-label\">Время:</span>");
        html.Append($"<span class=\"detail-value\">{step.StartedAtUtc:HH:mm:ss.fff} → {step.FinishedAtUtc:HH:mm:ss.fff}</span>");
        html.Append("</div>");
        
        if (step.Outputs is not null)
        {
            html.Append("<div class=\"detail-row\">");
            html.Append("<span class=\"detail-label\">Результат:</span>");
            html.Append($"<pre class=\"detail-code\">{EscapeHtml(FormatOutputs(step.Outputs))}</pre>");
            html.Append("</div>");
        }
        
        if (!string.IsNullOrEmpty(step.ErrorMessage))
        {
            html.Append("<div class=\"detail-row detail-row-error\">");
            html.Append("<span class=\"detail-label\">Ошибка:</span>");
            html.Append($"<pre class=\"detail-code error\">{EscapeHtml(MaskString(step.ErrorMessage))}</pre>");
            html.Append("</div>");
        }
        
        if (step.Logs.Count > 0)
        {
            html.Append("<div class=\"detail-row\">");
            html.Append("<span class=\"detail-label\">Логи:</span>");
            html.Append("<div class=\"logs\">");
            foreach (var log in step.Logs)
            {
                html.Append($"<div class=\"log-entry\">{EscapeHtml(MaskString(log))}</div>");
            }
            html.Append("</div>");
            html.Append("</div>");
        }
        
        html.Append("</div>");
        html.Append("</div>");
        
        return html.ToString();
    }

    private string FormatOutputs(object outputs)
    {
        if (outputs is null)
            return "null";
        
        if (outputs is System.Collections.IDictionary dict)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            foreach (System.Collections.DictionaryEntry entry in dict)
            {
                var key = entry.Key?.ToString() ?? "";
                var value = entry.Value switch
                {
                    string s => $"\"{MaskString(s)}\"",
                    null => "null",
                    _ => entry.Value.ToString()
                };
                sb.AppendLine($"  \"{key}\": {value},");
            }
            sb.Append("}");
            return sb.ToString();
        }
        
        return outputs.ToString() ?? "null";
    }

    private string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
            return $"{duration.TotalMilliseconds:F0}ms";
        if (duration.TotalMinutes < 1)
            return $"{duration.TotalSeconds:F2}s";
        return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
    }

    private string? MaskString(string? value)
    {
        if (string.IsNullOrEmpty(value) || _secretMasker is null)
            return value;
        return _secretMasker.Mask(value);
    }

    private static string EscapeHtml(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    private static string GetStyles()
    {
        return @"
<style>
* {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
}

body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
    background: #f5f7fa;
    color: #2c3e50;
    line-height: 1.6;
}

.container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 20px;
}

.header {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    padding: 30px;
    border-radius: 12px;
    margin-bottom: 20px;
    box-shadow: 0 4px 20px rgba(102, 126, 234, 0.3);
}

.workflow-name {
    font-size: 28px;
    font-weight: 700;
    margin-bottom: 10px;
}

.workflow-meta {
    display: flex;
    gap: 15px;
    align-items: center;
    flex-wrap: wrap;
}

.status-badge {
    padding: 6px 14px;
    border-radius: 20px;
    font-size: 14px;
    font-weight: 600;
    text-transform: uppercase;
}

.status-passed {
    background: #27ae60;
}

.status-failed {
    background: #e74c3c;
}

.duration, .timestamp {
    font-size: 14px;
    opacity: 0.9;
}

.summary {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
    gap: 15px;
    margin-bottom: 20px;
}

.summary-card {
    background: white;
    padding: 20px;
    border-radius: 10px;
    text-align: center;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.05);
}

.summary-value {
    font-size: 32px;
    font-weight: 700;
    color: #3498db;
}

.summary-label {
    font-size: 13px;
    color: #7f8c8d;
    text-transform: uppercase;
    margin-top: 5px;
}

.summary-card-passed .summary-value { color: #27ae60; }
.summary-card-failed .summary-value { color: #e74c3c; }
.summary-card-skipped .summary-value { color: #95a5a6; }

.steps-section {
    background: white;
    border-radius: 12px;
    padding: 25px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.05);
}

.section-title {
    font-size: 20px;
    font-weight: 600;
    margin-bottom: 20px;
    color: #2c3e50;
}

.steps-tree {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.step-node {
    border: 1px solid #e1e8ed;
    border-radius: 8px;
    overflow: hidden;
    transition: box-shadow 0.2s;
}

.step-node:hover {
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.step-node.passed { border-left: 4px solid #27ae60; }
.step-node.failed { border-left: 4px solid #e74c3c; }
.step-node.skipped { border-left: 4px solid #95a5a6; }

.step-header {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 15px;
    background: #f8f9fa;
    cursor: pointer;
    user-select: none;
}

.step-toggle {
    font-size: 12px;
    color: #7f8c8d;
    transition: transform 0.2s;
}

.step-node.expanded .step-toggle {
    transform: rotate(90deg);
}

.step-status {
    font-size: 16px;
}

.step-node.passed .step-status { color: #27ae60; }
.step-node.failed .step-status { color: #e74c3c; }
.step-node.skipped .step-status { color: #95a5a6; }

.step-id {
    font-weight: 600;
    color: #2c3e50;
}

.step-keyword {
    color: #8e44ad;
    font-family: 'Fira Code', monospace;
    font-size: 13px;
}

.step-duration {
    margin-left: auto;
    font-size: 13px;
    color: #7f8c8d;
}

.step-details {
    padding: 15px;
    background: white;
    border-top: 1px solid #e1e8ed;
}

.detail-row {
    margin-bottom: 12px;
}

.detail-row:last-child {
    margin-bottom: 0;
}

.detail-label {
    display: block;
    font-size: 12px;
    font-weight: 600;
    color: #7f8c8d;
    text-transform: uppercase;
    margin-bottom: 5px;
}

.detail-value {
    font-size: 14px;
    color: #2c3e50;
}

.detail-code {
    background: #f8f9fa;
    padding: 12px;
    border-radius: 6px;
    font-family: 'Fira Code', monospace;
    font-size: 13px;
    overflow-x: auto;
    white-space: pre-wrap;
    word-break: break-all;
}

.detail-code.error {
    background: #fee;
    color: #c0392b;
    border: 1px solid #fadbd8;
}

.detail-row-error .detail-label {
    color: #e74c3c;
}

.logs {
    background: #f8f9fa;
    border-radius: 6px;
    overflow: hidden;
}

.log-entry {
    padding: 8px 12px;
    font-family: 'Fira Code', monospace;
    font-size: 12px;
    border-bottom: 1px solid #e1e8ed;
}

.log-entry:last-child {
    border-bottom: none;
}

@media (max-width: 768px) {
    .container {
        padding: 10px;
    }
    
    .header {
        padding: 20px;
    }
    
    .workflow-name {
        font-size: 22px;
    }
    
    .workflow-meta {
        flex-direction: column;
        align-items: flex-start;
    }
    
    .step-header {
        flex-wrap: wrap;
    }
    
    .step-duration {
        margin-left: 0;
        width: 100%;
        margin-top: 5px;
    }
}
</style>";
    }

    private static string GetScripts()
    {
        return @"
<script>
function toggleStep(header) {
    const node = header.closest('.step-node');
    const details = node.querySelector('.step-details');
    const isExpanded = node.classList.contains('expanded');
    
    if (isExpanded) {
        details.style.display = 'none';
        node.classList.remove('expanded');
    } else {
        details.style.display = 'block';
        node.classList.add('expanded');
    }
}

document.addEventListener('DOMContentLoaded', function() {
    const failedNodes = document.querySelectorAll('.step-node.failed');
    failedNodes.forEach(node => {
        const header = node.querySelector('.step-header');
        toggleStep(header);
    });
});
</script>";
    }
}
