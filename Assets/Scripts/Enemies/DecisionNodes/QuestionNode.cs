using System;

public class QuestionNode : DecisionNode
{
    private readonly Func<EnemyContext, bool> question;
    private readonly DecisionNode trueNode;
    private readonly DecisionNode falseNode;

    public QuestionNode(Func<EnemyContext, bool> question, DecisionNode trueNode, DecisionNode falseNode)
    {
        this.question = question;
        this.trueNode = trueNode;
        this.falseNode = falseNode;
    }

    public override void Evaluate(EnemyController enemy, EnemyContext context)
    {
        if (question(context))
        {
            trueNode.Evaluate(enemy, context);
        }
        else
        {
            falseNode.Evaluate(enemy, context);
        }
    }
}
