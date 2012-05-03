﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NPEG.NonTerminals;
using NPEG.Terminals;

namespace NPEG.Tests
{
	[TestClass]
	public class PracticalExampleTests
	{
		public TestContext TestContext { get; set; }


		[TestMethod]
		public void PracticalExample_MathematicalFormula()
		{
			#region Composite

			var VALUE = new PrioritizedChoice(
				new CapturingGroup("VALUE",
				                   new OneOrMore(new CharacterClass {ClassExpression = "[0-9]"})
					)
				,
				new Sequence(
					new Literal {MatchText = "("},
					new RecursionCall("ParethesisFunction")
					)
					.Sequence(new Literal {MatchText = ")"})
				);

			var PRODUCT = new Sequence(
				VALUE,
				new Sequence(
					new CapturingGroup("SYMBOL",
					                   new PrioritizedChoice(
					                   	new Literal {MatchText = "*"},
					                   	new Literal {MatchText = "/"}
					                   	)
						),
					VALUE
					).Star()
				);

			var SUM = new Sequence(
				PRODUCT,
				new Sequence(
					new CapturingGroup("SYMBOL",
					                   new PrioritizedChoice(
					                   	new Literal {MatchText = "+"},
					                   	new Literal {MatchText = "-"}
					                   	)
						),
					PRODUCT
					).Star()
				);

			AExpression EXPRESSION = new RecursionCreate("ParethesisFunction", new CapturingGroup("EXPRESSION", SUM));

			#endregion

			String input = "((((12/3)+5-2*(81/9))+1))";

			var iterator = new StringInputIterator(input);
			var visitor = new NpegParserVisitor(iterator);
			EXPRESSION.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			AstNode node = visitor.AST;


			Assert.IsTrue(node.Token.Value == input);
		}


		[TestMethod]
		public void PracticalExample_BooleanAlgebra()
		{
			#region Composite

			//AND: */AND
			AExpression AND = new PrioritizedChoice(new Literal {MatchText = "*"}, new Literal {MatchText = "AND"});
			//NAND: ~*/NAND
			AExpression NAND = new PrioritizedChoice(new Literal {MatchText = "~*"}, new Literal {MatchText = "NAND"});


			//OR: +/OR
			AExpression OR = new PrioritizedChoice(new Literal {MatchText = "+"}, new Literal {MatchText = "OR"});
			//NOR: ~+/NOR
			AExpression NOR = new PrioritizedChoice(new Literal {MatchText = "~+"}, new Literal {MatchText = "NOR"});


			//XOR: ^/XOR
			AExpression XOR = new PrioritizedChoice(new Literal {MatchText = "^"}, new Literal {MatchText = "XOR"});
			//XNOR: ~^/XNOR
			AExpression XNOR = new PrioritizedChoice(new Literal {MatchText = "~^"}, new Literal {MatchText = "XNOR"});


			AExpression GATE = new CapturingGroup("GATE", new PrioritizedChoice(AND, NAND).Or(OR).Or(NOR).Or(XOR).Or(XNOR));


			// Variable: "[a-zA-Z0-9]+"  / '[a-zA-Z0-9]+' / [a-zA-Z]  
			AExpression VARIABLE = new PrioritizedChoice(
				new Sequence(
					new Literal {MatchText = "\""},
					new CapturingGroup("VARIABLE", new OneOrMore(new CharacterClass {ClassExpression = "[a-zA-Z0-9]"}))
					).Sequence(new Literal {MatchText = "\""}),
				new Sequence(
					new Literal {MatchText = "'"},
					new CapturingGroup("VARIABLE", new OneOrMore(new CharacterClass {ClassExpression = "[a-zA-Z0-9]"}))
					).Sequence(new Literal {MatchText = "'"})
				).Or(
					new CapturingGroup("VARIABLE", new CharacterClass {ClassExpression = "[a-zA-Z]"})
				);

			// Variable: Variable / !Variable
			VARIABLE = new PrioritizedChoice(
				VARIABLE
				,
				new CapturingGroup("INVERTOR",
				                   new Sequence(
				                   	new Literal {MatchText = "!"},
				                   	VARIABLE
				                   	)
					)
				);


			// Variable: Variable / Expression / !Expression
			VARIABLE = new PrioritizedChoice(
				VARIABLE
				,
				new Sequence(
					new Literal {MatchText = "("},
					new RecursionCall("RECURSIONEXPRESSION")
					).Sequence(new Literal {MatchText = ")"})
				).Or(
					new CapturingGroup("INVERTOR",
					                   new Sequence(
					                   	new Literal {MatchText = "!"}
					                   	,
					                   	new Sequence(
					                   		new Literal {MatchText = "("},
					                   		new RecursionCall("RECURSIONEXPRESSION")
					                   		).Sequence(new Literal {MatchText = ")"})
					                   	)
						)
				);


			AExpression Root = new CapturingGroup("BOOLEANEQUATION",
			                                      new Sequence(
			                                      	new RecursionCreate("RECURSIONEXPRESSION",
			                                      	                    //Expression: Variable ((AND|NAND|OR|NOR|XOR|XNOR) Variable)* 
			                                      	                    new Sequence(VARIABLE, new Sequence(GATE, VARIABLE).Star())
			                                      		)
			                                      	,
			                                      	// ensure reaches end of file
			                                      	new NotPredicate(new AnyCharacter())
			                                      	)
				);

			#endregion

			// single variable
			String input = "A*!B+!A*B";
			var iterator = new StringInputIterator(input);
			var visitor = new NpegParserVisitor(iterator);
			Root.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			AstNode node = visitor.AST;
			Assert.IsTrue(node.Token.Name == "BOOLEANEQUATION");
			Assert.IsTrue(node.Children[0].Token.Name == "VARIABLE");
			Assert.IsTrue(node.Children[0].Token.Value == "A");
			Assert.IsTrue(node.Children[1].Token.Name == "GATE");
			Assert.IsTrue(node.Children[1].Token.Value == "*");
			Assert.IsTrue(node.Children[2].Token.Name == "INVERTOR");
			Assert.IsTrue(node.Children[2].Children[0].Token.Name == "VARIABLE");
			Assert.IsTrue(node.Children[2].Children[0].Token.Value == "B");
			Assert.IsTrue(node.Children[3].Token.Name == "GATE");
			Assert.IsTrue(node.Children[4].Token.Name == "INVERTOR");
			Assert.IsTrue(node.Children[4].Children[0].Token.Name == "VARIABLE");
			Assert.IsTrue(node.Children[4].Children[0].Token.Value == "A");
			Assert.IsTrue(node.Children[5].Token.Name == "GATE");
			Assert.IsTrue(node.Children[6].Token.Name == "VARIABLE");
			Assert.IsTrue(node.Children[6].Token.Value == "B");

			// quoted variable
			iterator = new StringInputIterator("'aA'*!'bB'+!'aA'*'bB'");
			visitor = new NpegParserVisitor(iterator);
			Root.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
			Assert.IsTrue(node.Token.Name == "BOOLEANEQUATION");
			Assert.IsTrue(node.Children[0].Token.Name == "VARIABLE");
			Assert.IsTrue(node.Children[0].Token.Value == "aA");
			Assert.IsTrue(node.Children[1].Token.Name == "GATE");
			Assert.IsTrue(node.Children[1].Token.Value == "*");
			Assert.IsTrue(node.Children[2].Token.Name == "INVERTOR");
			Assert.IsTrue(node.Children[2].Children[0].Token.Name == "VARIABLE");
			Assert.IsTrue(node.Children[2].Children[0].Token.Value == "bB");
			Assert.IsTrue(node.Children[3].Token.Name == "GATE");
			Assert.IsTrue(node.Children[4].Token.Name == "INVERTOR");
			Assert.IsTrue(node.Children[4].Children[0].Token.Name == "VARIABLE");
			Assert.IsTrue(node.Children[4].Children[0].Token.Value == "aA");
			Assert.IsTrue(node.Children[5].Token.Name == "GATE");
			Assert.IsTrue(node.Children[6].Token.Name == "VARIABLE");
			Assert.IsTrue(node.Children[6].Token.Value == "bB");


			// expression + gate + variable .star()
			iterator = new StringInputIterator("A*!B*C+!A*B*C");
			visitor = new NpegParserVisitor(iterator);
			Root.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning             Assert.IsTrue(node.Token.Value == input);

			// parethesis
			iterator = new StringInputIterator("((A)*(!B)+(!A)*(B))");
			visitor = new NpegParserVisitor(iterator);
			Root.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning            Assert.IsTrue(node.Token.Value == input);

			iterator = new StringInputIterator("((A)*!(B)+!(A)*(B))");
			visitor = new NpegParserVisitor(iterator);
			Root.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning            Assert.IsTrue(node.Token.Value == input);

			iterator = new StringInputIterator("((A)*(!(B))+(!(A))*(B))");
			visitor = new NpegParserVisitor(iterator);
			Root.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning            Assert.IsTrue(node.Token.Value == input);

			iterator = new StringInputIterator("(!X*Y*!Z)");
			visitor = new NpegParserVisitor(iterator);
			Root.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning             Assert.IsTrue(node.Token.Value == input);

			iterator = new StringInputIterator("(!X*Y*!Z)+(!X*Y*Z)");
			visitor = new NpegParserVisitor(iterator);
			Root.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning             Assert.IsTrue(node.Token.Value == input);

			iterator = new StringInputIterator("(X*Z)");
			visitor = new NpegParserVisitor(iterator);
			Root.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning             Assert.IsTrue(node.Token.Value == input);

			iterator = new StringInputIterator("(!X*Y*!Z)+(!X*Y*Z)+(X*Z)");
			visitor = new NpegParserVisitor(iterator);
			Root.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning            Assert.IsTrue(node.Token.Value == input);

			iterator = new StringInputIterator("((((!X*Y*Z)+(!X*Y*!Z)+(X*Z))))");
			visitor = new NpegParserVisitor(iterator);
			Root.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning            Assert.IsTrue(node.Token.Value == input);
		}


		[TestMethod]
		public void PracticalExample_PhoneNumber()
		{
			#region terminals

			AExpression Digits = new CharacterClass {ClassExpression = "[0-9]"};
			AExpression Hyphen = new Literal {MatchText = "-"};

			#endregion

			#region nonterminals

			AExpression ThreeDigitCode = new CapturingGroup("ThreeDigitCode", new Sequence(Digits, Digits).Sequence(Digits));

			AExpression FourDigitCode = new CapturingGroup("FourDigitCode",
			                                               new Sequence(Digits, Digits).Sequence(Digits).Sequence(Digits));

			AExpression PhoneNumber = new CapturingGroup("PhoneNumber",
			                                             new Sequence(ThreeDigitCode, Hyphen)
			                                             	.Sequence(ThreeDigitCode)
			                                             	.Sequence(Hyphen)
			                                             	.Sequence(FourDigitCode)
				);

			#endregion

			String input = "123-456-7890";


			// Test Manual Composite
			var visitor = new NpegParserVisitor(
				new StringInputIterator(input)
				);
			PhoneNumber.Accept(visitor);

			Assert.IsTrue(visitor.IsMatch);
			AstNode node = visitor.AST;
			Assert.IsTrue(node.Token.Name == "PhoneNumber");
			Assert.IsTrue(node.Token.Value == "123-456-7890");
			Assert.IsTrue(node.Children[0].Token.Name == "ThreeDigitCode");
			Assert.IsTrue(node.Children[0].Token.Value == "123");
			Assert.IsTrue(node.Children[1].Token.Name == "ThreeDigitCode");
			Assert.IsTrue(node.Children[1].Token.Value == "456");
			Assert.IsTrue(node.Children[2].Token.Name == "FourDigitCode");
			Assert.IsTrue(node.Children[2].Token.Value == "7890");
		}
	}
}