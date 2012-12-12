﻿using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NPEG.ApplicationExceptions;
using NPEG.Extensions;
using NPEG.GrammarInterpreter;

namespace NPEG.Tests
{
	[TestClass]
	public class GrammarInterpreterTests
	{
		public TestContext TestContext { get; set; }

		[TestMethod]
		public void PEGrammar_Literal()
		{
			AExpression caseSensitive = PEGrammar.Load(@"(?<Expression>): 'Hello World';");

			var input = "hello world";
			var bytes = Encoding.UTF8.GetBytes(input);
			var iterator = new ByteInputIterator(bytes);
			var visitor = new NpegParserVisitor(iterator);
			caseSensitive.Accept(visitor);
			Assert.IsFalse(visitor.IsMatch);

			AExpression notCaseSensitive = PEGrammar.Load(@"(?<Expression>): 'Hello World'\i;");
			input = "hello world";
			bytes = Encoding.UTF8.GetBytes(input);
			iterator = new ByteInputIterator(bytes);
			visitor = new NpegParserVisitor(iterator);
			notCaseSensitive.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			AstNode node = visitor.AST;
			Assert.IsTrue(node.Token.Name == "Expression");
			Assert.IsTrue(node.Token.Value(iterator) == input);


			// not sure if it would be better to use verbatim identifier @"" for escaping
			// escape back slash inside double quotes
			input = @"\";
			AExpression escape = PEGrammar.Load(@"(?<Literal>): ""\\"";");
			bytes = Encoding.UTF8.GetBytes(input);
			iterator = new ByteInputIterator(bytes);
			visitor = new NpegParserVisitor(iterator);
			escape.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			Assert.IsTrue(@"\" == visitor.AST.Token.Value(iterator));

			input = @"\";
			escape = PEGrammar.Load(@"(?<Literal>): '\\';");
			bytes = Encoding.UTF8.GetBytes(input);
			iterator = new ByteInputIterator(bytes);
			visitor = new NpegParserVisitor(iterator);
			escape.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			Assert.IsTrue(@"\" == visitor.AST.Token.Value(iterator));
		}


		[TestMethod]
		[Timeout(2000)]
		[ExpectedException(typeof (InfiniteLoopDetectedException))]
		public void PEGrammar_ZeroOrMore_InfiniteLoopTest()
		{
			AExpression caseSensitive = PEGrammar.Load(@"(?<Expression>): (.*)*;");

			var input = "";
			var bytes = Encoding.UTF8.GetBytes(input);
			var visitor = new NpegParserVisitor(new ByteInputIterator(bytes));
			caseSensitive.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
		}

		[TestMethod]
		[Timeout(2000)]
		[ExpectedException(typeof (InfiniteLoopDetectedException))]
		public void PEGrammar_LimitingRepetition_InfiniteLoopTest()
		{
			string input = "";
			AExpression caseSensitive = PEGrammar.Load(@"(?<Expression>): (.*){0,};");

			var bytes = Encoding.UTF8.GetBytes(input);
			var visitor = new NpegParserVisitor(new ByteInputIterator(bytes));
			caseSensitive.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
		}

		[TestMethod]
		[Timeout(2000)]
		[ExpectedException(typeof (InfiniteLoopDetectedException))]
		public void PEGrammar_OneOrMore_InfiniteLoopTest()
		{
			string input = "";
			AExpression caseSensitive = PEGrammar.Load(@"(?<Expression>): (.*)+;");

			var bytes = Encoding.UTF8.GetBytes(input);
			var visitor = new NpegParserVisitor(new ByteInputIterator(bytes));
			caseSensitive.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
		}

#warning commented out - do we want to throw an InfiniteLoopDetectedException or do we want to instead move forward with the bad grammar?
		//[TestMethod]
		//public void PEGrammar_OneOrMore_WithPredicate_DoesNotCauseInfiniteLoop()
		//{
		//    string input = "";
		//    AExpression caseSensitive = PEGrammar.Load(@"(?<Expression>): (.*)+;");

		//    var visitor = new NpegParserVisitor(new StringInputIterator(input));
		//    caseSensitive.Accept(visitor);
		//    Assert.IsTrue(visitor.IsMatch);
		//}


		// Root node must always be a capturing group.
		[TestMethod]
		public void PEGrammar_LimitingRepetition()
		{
			var grammar =
				@"
                                (?<ThreeDigitCode>): [0-9]{3,3};
                                (?<PhoneNumber>): ThreeDigitCode '-' ThreeDigitCode '-' (?<FourDigitCode>[0-9]{4});
                              ";

			var ROOT = PEGrammar.Load(grammar);

			var input = "123-456-7890";
			var bytes = Encoding.UTF8.GetBytes(input);
			var iterator = new ByteInputIterator(bytes);
			var visitor = new NpegParserVisitor(iterator);
			ROOT.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			AstNode node = visitor.AST;

			Assert.IsTrue(node.Token.Name == "PhoneNumber");
			Assert.IsTrue(node.Token.Value(iterator) == input);
			Assert.IsTrue(node.Children[0].Token.Name == "ThreeDigitCode");
			Assert.IsTrue(node.Children[0].Token.Value(iterator) == "123");
			Assert.IsTrue(node.Children[1].Token.Name == "ThreeDigitCode");
			Assert.IsTrue(node.Children[1].Token.Value(iterator) == "456");
			Assert.IsTrue(node.Children[2].Token.Name == "FourDigitCode");
			Assert.IsTrue(node.Children[2].Token.Value(iterator) == "7890");
		}


		[TestMethod]
		public void PEGrammar_PhoneNumber()
		{
			var input = "123-456-7890";

			var PhoneNumber = PEGrammar.Load(
				@"
                        (?<ThreeDigitCode>): [0-9] [0-9] [0-9];
                        (?<FourDigitCode>): [0-9] [0-9] [0-9] [0-9];
                        (?<PhoneNumber>): ThreeDigitCode '-' ThreeDigitCode '-' FourDigitCode;
                    "
					.Trim());

			var bytes = Encoding.UTF8.GetBytes(input);
			var iterator = new ByteInputIterator(bytes);
			var visitor = new NpegParserVisitor(iterator);
			PhoneNumber.Accept(visitor);

			Assert.IsTrue(visitor.IsMatch);
			AstNode node = visitor.AST;
			Assert.IsTrue(node.Token.Name == "PhoneNumber");
			Assert.IsTrue(node.Token.Value(iterator) == input);
			Assert.IsTrue(node.Children[0].Token.Name == "ThreeDigitCode");
			Assert.IsTrue(node.Children[0].Token.Value(iterator) == "123");
			Assert.IsTrue(node.Children[1].Token.Name == "ThreeDigitCode");
			Assert.IsTrue(node.Children[1].Token.Value(iterator) == "456");
			Assert.IsTrue(node.Children[2].Token.Name == "FourDigitCode");
			Assert.IsTrue(node.Children[2].Token.Value(iterator) == "7890");
		}

		[TestMethod]
		public void PEGrammar_RecursiveParentheses()
		{
			var input = "((((((123))))))";
			var bytes = Encoding.UTF8.GetBytes(input);

			AExpression ROOT = PEGrammar.Load(
				@"
                        (?<DIGITS>): ([0-9])+;
                        (?<ENCLOSEDDIGITS>): '(' ParethesisFunction ')';
                        ParethesisFunction: (DIGITS / ENCLOSEDDIGITS);
                        (?<RECURSIONTEST>): ParethesisFunction;
                    "
					.Trim());

			var iterator = new ByteInputIterator(bytes);
			var visitor = new NpegParserVisitor(iterator);
			ROOT.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			AstNode node = visitor.AST;
			Assert.IsTrue(node.Token.Value(iterator) == input);
		}

		[TestMethod]
		public void PEGrammar_MathematicalFormula_Recursion()
		{
			AExpression ROOT = PEGrammar.Load(
				@"
                    (?<Value>): [0-9]+ / '(' Expr ')';
                    (?<Product>): Value ((?<Symbol>'*' / '/') Value)*;
                    (?<Sum>): Product ((?<Symbol>'+' / '-') Product)*;
                    (?<Expr>): Sum;
                "
				);

			String input = "((((12/3)+5-2*(81/9))+1))";
			var bytes = Encoding.UTF8.GetBytes(input);
			var iterator = new ByteInputIterator(bytes);
			var visitor = new NpegParserVisitor(iterator);

			ROOT.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			AstNode node = visitor.AST;
			Assert.IsTrue(node.Token.Value(iterator) == input);
#warning does not specify expected tree
		}


		[TestMethod]
		[ExpectedException(typeof (ParsingFatalTerminalException))]
		public void PEGrammar_Interpreter_Fatal()
		{
			AExpression ROOT = PEGrammar.Load(
				@"
                    (?<Value>): Fatal<'error'>;
                "
				);

			String input = " ";
			var bytes = Encoding.UTF8.GetBytes(input);
			var iterator = new ByteInputIterator(bytes);
			var visitor = new NpegParserVisitor(iterator);

			ROOT.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
		}


		[TestMethod]
		public void PEGrammar_Interpreter_Warn()
		{
			AExpression ROOT = PEGrammar.Load(
				@"
                    (?<Value>): Warn<'warning'>;
                "
				);

			String input = " ";
			var bytes = Encoding.UTF8.GetBytes(input);
			var iterator = new ByteInputIterator(bytes);
			var visitor = new NpegParserVisitor(iterator);

			ROOT.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			Assert.IsTrue(visitor.Warnings.Count == 1);
		}


		[TestMethod]
		public void PEGrammar_Interpreter_CodePoint()
		{
			AExpression ROOT = PEGrammar.Load(
				@"
                    (?<Value>): #x20;
                "
				);

			String input = " ";
			var bytes = Encoding.UTF8.GetBytes(input);
			var iterator = new ByteInputIterator(bytes);
			var visitor = new NpegParserVisitor(iterator);

			ROOT.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
		}


		[TestMethod]
		public void PEGrammar_BooleanAlgebra()
		{
			String grammar =
				@"
					S: [\s]+;
                    (?<Gate>): ('*' / 'AND') / ('~*' / 'NAND') / ('+' / 'OR') / ('~+' / 'NOR') / ('^' / 'XOR') / ('~^' / 'XNOR');
                    ValidVariable: '""' (?<Variable>[a-zA-Z0-9]+) '""'  / '\'' (?<Variable>[a-zA-Z0-9]+) '\'' / (?<Variable>[a-zA-Z]);
                    VarProjection1: ValidVariable /  (?<Invertor>'!' ValidVariable);
                    VarProjection2: VarProjection1 / '(' Expression ')' / (?<Invertor>'!' '(' Expression ')');
                    Expression: S? VarProjection2 S? (Gate S? VarProjection2 S?)*;
                    (?<BooleanEquation>): Expression !.;
                "
					.Trim();

			AExpression ROOT = PEGrammar.Load(grammar);

			// single variable
			var input = ("A*!B+!A*B");
			var bytes = Encoding.UTF8.GetBytes(input);
			var iterator = new ByteInputIterator(bytes);
			var visitor = new NpegParserVisitor(iterator);
			ROOT.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			AstNode node = visitor.AST;
#warning Assert.IsTrue(node.Token.Value == input);


			// quoted variable
			input = ("'aA'*!'bB'+!'aA'*'bB'");
			bytes = Encoding.UTF8.GetBytes(input);
			iterator = new ByteInputIterator(bytes);
			visitor = new NpegParserVisitor(iterator);
			ROOT.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning             Assert.IsTrue(node.Token.Value == input);

			// expression + gate + variable .star()
			input = ("A*!B*C+!A*B*C");
			bytes = Encoding.UTF8.GetBytes(input);
			iterator = new ByteInputIterator(bytes);
			visitor = new NpegParserVisitor(iterator);
			ROOT.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning             Assert.IsTrue(node.Token.Value == input);

			// parethesis
			input = ("((A)*(!B)+(!A)*(B))");
			bytes = Encoding.UTF8.GetBytes(input);
			iterator = new ByteInputIterator(bytes);
			visitor = new NpegParserVisitor(iterator);
			ROOT.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning            Assert.IsTrue(node.Token.Value == input);

			input = ("((A)*!(B)+!(A)*(B))");
			bytes = Encoding.UTF8.GetBytes(input);
			iterator = new ByteInputIterator(bytes);
			visitor = new NpegParserVisitor(iterator);
			ROOT.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning            Assert.IsTrue(node.Token.Value == input);

			input = ("((A)*(!(B))+(!(A))*(B))");
			bytes = Encoding.UTF8.GetBytes(input);
			iterator = new ByteInputIterator(bytes);
			visitor = new NpegParserVisitor(iterator);
			ROOT.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning            Assert.IsTrue(node.Token.Value == input);

			input = ("(!X*Y*!Z)");
			bytes = Encoding.UTF8.GetBytes(input);
			iterator = new ByteInputIterator(bytes);
			visitor = new NpegParserVisitor(iterator);
			ROOT.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning             Assert.IsTrue(node.Token.Value == input);

			input = ("(!X*Y*!Z)+(!X*Y*Z)");
			bytes = Encoding.UTF8.GetBytes(input);
			iterator = new ByteInputIterator(bytes);
			visitor = new NpegParserVisitor(iterator);
			ROOT.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning             Assert.IsTrue(node.Token.Value == input);

			input = ("(X*Z)");
			bytes = Encoding.UTF8.GetBytes(input);
			iterator = new ByteInputIterator(bytes);
			visitor = new NpegParserVisitor(iterator);
			ROOT.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning             Assert.IsTrue(node.Token.Value == input);

			input = ("(!X*Y*!Z)+(!X*Y*Z)+(X*Z)");
			bytes = Encoding.UTF8.GetBytes(input);
			iterator = new ByteInputIterator(bytes);
			visitor = new NpegParserVisitor(iterator);
			ROOT.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning            Assert.IsTrue(node.Token.Value == input);

			input = ("((((!X*Y*Z)+(!X*Y*!Z)+(X*Z))))");
			bytes = Encoding.UTF8.GetBytes(input);
			iterator = new ByteInputIterator(bytes);
			visitor = new NpegParserVisitor(iterator);
			ROOT.Accept(visitor);
			Assert.IsTrue(visitor.IsMatch);
			node = visitor.AST;
#warning            Assert.IsTrue(node.Token.Value == input);
		}
	}
}