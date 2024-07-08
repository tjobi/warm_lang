namespace WarmLangCompiler.Binding.ControlFlow;

internal sealed record Edge(BasicBlock From, BasicBlock To, BoundExpression? Condition = null);
