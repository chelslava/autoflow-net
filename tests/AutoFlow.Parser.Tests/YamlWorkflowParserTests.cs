// Этот код нужен для тестирования YAML parser.
using AutoFlow.Abstractions;
using AutoFlow.Parser;
using Xunit;

namespace AutoFlow.Parser.Tests;

public sealed class YamlWorkflowParserTests
{
    private readonly YamlWorkflowParser _parser = new();

    [Fact]
    public void Parse_ShouldParseMinimalDocument()
    {
        var yaml = """
            schema_version: 1
            name: test_flow
            tasks:
              main:
                steps:
                  - step:
                      id: step1
                      uses: log.info
                      with:
                        message: "Hello"
            """;

        var document = _parser.Parse(yaml);

        Assert.Equal(1, document.SchemaVersion);
        Assert.Equal("test_flow", document.Name);
        Assert.Single(document.Tasks);
        Assert.True(document.Tasks.ContainsKey("main"));
        Assert.Single(document.Tasks["main"].Steps);
    }

    [Fact]
    public void Parse_ShouldParseVariables()
    {
        var yaml = """
            schema_version: 1
            name: test_flow
            variables:
              app_name: AutoFlow
              count: 42
              enabled: true
            tasks:
              main:
                steps: []
            """;

        var document = _parser.Parse(yaml);

        Assert.Equal(3, document.Variables.Count);
        Assert.Equal("AutoFlow", document.Variables["app_name"]);
        Assert.Equal(42, document.Variables["count"]);
        Assert.Equal(true, document.Variables["enabled"]);
    }

    [Fact]
    public void Parse_ShouldParseTaskInputs()
    {
        var yaml = """
            schema_version: 1
            name: test_flow
            tasks:
              login:
                inputs:
                  url:
                    type: string
                    required: true
                  timeout:
                    type: int
                    required: false
                steps: []
            """;

        var document = _parser.Parse(yaml);

        var loginTask = document.Tasks["login"];
        Assert.Equal(2, loginTask.Inputs.Count);
        Assert.Equal("string", loginTask.Inputs["url"].Type);
        Assert.True(loginTask.Inputs["url"].Required);
        Assert.Equal("int", loginTask.Inputs["timeout"].Type);
        Assert.False(loginTask.Inputs["timeout"].Required);
    }

    [Fact]
    public void Parse_ShouldParseTaskOutputs()
    {
        var yaml = """
            schema_version: 1
            name: test_flow
            tasks:
              login:
                outputs:
                  token:
                    type: string
                  user_id:
                    type: int
                steps: []
            """;

        var document = _parser.Parse(yaml);

        var loginTask = document.Tasks["login"];
        Assert.Equal(2, loginTask.Outputs.Count);
        Assert.Equal("string", loginTask.Outputs["token"].Type);
        Assert.Equal("int", loginTask.Outputs["user_id"].Type);
    }

    [Fact]
    public void Parse_ShouldParseStepNode()
    {
        var yaml = """
            schema_version: 1
            name: test_flow
            tasks:
              main:
                steps:
                  - step:
                      id: open_browser
                      uses: browser.open
                      with:
                        url: https://example.com
                        headless: true
                      save_as: browser
                      timeout: 30s
                      continue_on_error: false
                      retry:
                        attempts: 3
                        delay: 5s
            """;

        var document = _parser.Parse(yaml);

        var step = document.Tasks["main"].Steps[0] as StepNode;
        Assert.NotNull(step);
        Assert.Equal("open_browser", step!.Id);
        Assert.Equal("browser.open", step.Uses);
        Assert.Equal("https://example.com", step.With["url"]);
        Assert.Equal(true, step.With["headless"]);
        Assert.Equal("browser", step.SaveAs);
        Assert.Equal("30s", step.Timeout);
        Assert.False(step.ContinueOnError);
        Assert.NotNull(step.Retry);
        Assert.Equal(3, step.Retry!.Attempts);
        Assert.Equal("5s", step.Retry.Delay);
    }

    [Fact]
    public void Parse_ShouldParseIfNode()
    {
        var yaml = """
            schema_version: 1
            name: test_flow
            tasks:
              main:
                steps:
                  - if:
                      condition:
                        var: env
                        op: eq
                        value: prod
                      then:
                        - step:
                            id: deploy_prod
                            uses: deploy.run
                      else:
                        - step:
                            id: skip_deploy
                            uses: log.info
                            with:
                              message: "Skipping deploy"
            """;

        var document = _parser.Parse(yaml);

        var ifNode = document.Tasks["main"].Steps[0] as IfNode;
        Assert.NotNull(ifNode);
        Assert.NotNull(ifNode!.Condition);
        Assert.Equal("env", ifNode.Condition.Var);
        Assert.Equal("eq", ifNode.Condition.Op);
        Assert.Equal("prod", ifNode.Condition.Value);
        Assert.Single(ifNode.Then);
        Assert.Single(ifNode.Else);
    }

    [Fact]
    public void Parse_ShouldParseForEachNode()
    {
        var yaml = """
            schema_version: 1
            name: test_flow
            tasks:
              main:
                steps:
                  - for_each:
                      items: ${users}
                      as: user
                      steps:
                        - step:
                            id: process_user
                            uses: user.process
                            with:
                              user_id: ${user.id}
            """;

        var document = _parser.Parse(yaml);

        var forEachNode = document.Tasks["main"].Steps[0] as ForEachNode;
        Assert.NotNull(forEachNode);
        Assert.Equal("${users}", forEachNode!.ItemsExpression);
        Assert.Equal("user", forEachNode.As);
        Assert.Single(forEachNode.Steps);
    }

    [Fact]
    public void Parse_ShouldParseCallNode()
    {
        var yaml = """
            schema_version: 1
            name: test_flow
            tasks:
              login:
                steps:
                  - step:
                      id: do_login
                      uses: auth.login
              main:
                steps:
                  - call:
                      id: call_login
                      task: login
                      inputs:
                        url: https://api.example.com
                      save_as: login_result
            """;

        var document = _parser.Parse(yaml);

        var callNode = document.Tasks["main"].Steps[0] as CallNode;
        Assert.NotNull(callNode);
        Assert.Equal("call_login", callNode!.Id);
        Assert.Equal("login", callNode.Task);
        Assert.Equal("https://api.example.com", callNode.Inputs["url"]);
        Assert.Equal("login_result", callNode.SaveAs);
    }

    [Fact]
    public void Parse_ShouldParseGroupNode()
    {
        var yaml = """
            schema_version: 1
            name: test_flow
            tasks:
              main:
                steps:
                  - group:
                      name: Setup
                      steps:
                        - step:
                            id: step1
                            uses: log.info
                            with:
                              message: "Step 1"
                        - step:
                            id: step2
                            uses: log.info
                            with:
                              message: "Step 2"
            """;

        var document = _parser.Parse(yaml);

        var groupNode = document.Tasks["main"].Steps[0] as GroupNode;
        Assert.NotNull(groupNode);
        Assert.Equal("Setup", groupNode!.Name);
        Assert.Equal(2, groupNode.Steps.Count);
    }

    [Fact]
    public void Parse_ShouldThrowOnEmptyYaml()
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse(""));
        Assert.Throws<ArgumentException>(() => _parser.Parse("   "));
        Assert.Throws<ArgumentException>(() => _parser.Parse(null!));
    }

    [Fact]
    public void Parse_ShouldThrowOnMissingName()
    {
        var yaml = """
            schema_version: 1
            tasks:
              main:
                steps: []
            """;

        Assert.Throws<ArgumentException>(() => _parser.Parse(yaml));
    }

    [Fact]
    public void Parse_ShouldThrowOnMissingTasks()
    {
        var yaml = """
            schema_version: 1
            name: test_flow
            """;

        Assert.Throws<ArgumentException>(() => _parser.Parse(yaml));
    }

    [Fact]
    public void Parse_ShouldThrowOnMissingStepId()
    {
        var yaml = """
            schema_version: 1
            name: test_flow
            tasks:
              main:
                steps:
                  - step:
                      uses: log.info
            """;

        Assert.Throws<ArgumentException>(() => _parser.Parse(yaml));
    }

    [Fact]
    public void Parse_ShouldThrowOnMissingStepUses()
    {
        var yaml = """
            schema_version: 1
            name: test_flow
            tasks:
              main:
                steps:
                  - step:
                      id: step1
            """;

        Assert.Throws<ArgumentException>(() => _parser.Parse(yaml));
    }

    [Fact]
    public void Parse_ShouldParseComplexWorkflow()
    {
        var yaml = """
            schema_version: 1
            name: complex_flow
            
            variables:
              base_url: https://api.example.com
              timeout: 30
            
            tasks:
              login:
                inputs:
                  username:
                    type: string
                    required: true
                  password:
                    type: string
                    required: true
                    secret: true
                outputs:
                  token:
                    type: string
                steps:
                  - step:
                      id: auth
                      uses: http.post
                      with:
                        url: ${base_url}/auth
                        body:
                          username: ${inputs.username}
                          password: ${inputs.password}
                      save_as: auth_result
            
              main:
                steps:
                  - call:
                      id: do_login
                      task: login
                      inputs:
                        username: admin
                        password: secret
                      save_as: login_result
                  
                  - if:
                      condition:
                        var: env
                        op: eq
                        value: prod
                      then:
                        - step:
                            id: deploy
                            uses: deploy.run
                  
                  - for_each:
                      items: ${users}
                      as: user
                      steps:
                        - step:
                            id: process
                            uses: user.process
                            with:
                              user_id: ${user.id}
            """;

        var document = _parser.Parse(yaml);

        Assert.Equal("complex_flow", document.Name);
        Assert.Equal(2, document.Variables.Count);
        Assert.Equal(2, document.Tasks.Count);
        Assert.True(document.Tasks.ContainsKey("login"));
        Assert.True(document.Tasks.ContainsKey("main"));
        
        var loginTask = document.Tasks["login"];
        Assert.Equal(2, loginTask.Inputs.Count);
        Assert.True(loginTask.Inputs["password"].Secret);
        Assert.Single(loginTask.Steps);
        
        var mainTask = document.Tasks["main"];
        Assert.Equal(3, mainTask.Steps.Count);
        
        Assert.IsType<CallNode>(mainTask.Steps[0]);
        Assert.IsType<IfNode>(mainTask.Steps[1]);
        Assert.IsType<ForEachNode>(mainTask.Steps[2]);
    }
}
