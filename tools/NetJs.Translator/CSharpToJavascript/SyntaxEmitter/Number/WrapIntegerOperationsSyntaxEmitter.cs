using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Numbers
{
    /// <summary>
    /// All int32 or uint32 operations must produce int32 or uint32, clipped to the range of int32 or uint32.
    /// </summary>
    sealed class WrapIntegerOperationsSyntaxEmitter : SyntaxEmitter<BinaryExpressionSyntax>
    {
        public override bool TryEmit(BinaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.AddExpression) || node.IsKind(SyntaxKind.SubtractExpression) || node.IsKind(SyntaxKind.MultiplyExpression))
            {
                var lhsType = visitor.Global.TryGetTypeSymbol(node.Left, visitor);
                var rhsType = visitor.Global.TryGetTypeSymbol(node.Right, visitor);
                if (lhsType != null &&
                    rhsType != null &&
                    lhsType.IsIntegerNumericType() &&
                    rhsType.IsIntegerNumericType())
                {
                    foreach (var sm in visitor.SemanticModels)
                    {
                        if (node.SyntaxTree == sm.SyntaxTree)
                        {
                            if (!ImplicitConversionSyntaxEmitter.NumberImplicitlyConvertsToLong(node, sm, visitor, null) &&
                                !ImplicitConversionSyntaxEmitter.NumberImplicitlyConvertsToLong(node.Left, sm, visitor, null) &&
                                !ImplicitConversionSyntaxEmitter.NumberImplicitlyConvertsToLong(node.Right, sm, visitor, null))
                            {
                                bool isChecked = visitor.Global.Evaluate("checked", visitor) != null;
                                //bool leftSigned = lhsType.IsSignedNumericType();
                                //bool rightSigned = rhsType.IsSignedNumericType();
                                ////if one of the operand is actually unsigned literal, it isnt signed
                                //if (leftSigned && node.Left.IsKind(SyntaxKind.NumericLiteralExpression) && node.Left is LiteralExpressionSyntax ltl)
                                //{
                                //    if (!ltl.Token.ValueText.StartsWith("-"))
                                //    {
                                //        leftSigned = false;
                                //    }
                                //}
                                //if (rightSigned && node.Right.IsKind(SyntaxKind.NumericLiteralExpression) && node.Right is LiteralExpressionSyntax ltr)
                                //{
                                //    if (!ltr.Token.ValueText.StartsWith("-"))
                                //    {
                                //        rightSigned = false;
                                //    }
                                //}
                                //var isSignedResult = leftSigned || rightSigned || (!leftSigned && !rightSigned && node.IsKind(SyntaxKind.SubtractExpression));
                                SpecialType resultType = SpecialType.None;
                                if (node.IsKind(SyntaxKind.SubtractExpression))
                                {
                                    resultType = (lhsType.SpecialType, rhsType.SpecialType) switch
                                    {
                                        // 1. Both Unsigned 32-bit
                                        (SpecialType.System_UInt32, SpecialType.System_UInt32) => SpecialType.System_UInt32,

                                        // 2. Unsigned 32-bit mixed with smaller unsigned/char types -> Resolves to uint
                                        (SpecialType.System_UInt32, SpecialType.System_Char) => SpecialType.System_UInt32,
                                        (SpecialType.System_Char, SpecialType.System_UInt32) => SpecialType.System_UInt32,
                                        (SpecialType.System_UInt32, SpecialType.System_UInt16) => SpecialType.System_UInt32,
                                        (SpecialType.System_UInt16, SpecialType.System_UInt32) => SpecialType.System_UInt32,
                                        (SpecialType.System_UInt32, SpecialType.System_Byte) => SpecialType.System_UInt32,
                                        (SpecialType.System_Byte, SpecialType.System_UInt32) => SpecialType.System_UInt32,

                                        // 3. Mixed Sign 32-bit (int vs uint / uint vs int) -> Promotes to long
                                        // Note: Also captures uint mixed with smaller signed types (short, sbyte)
                                        (SpecialType.System_Int32, SpecialType.System_UInt32) => SpecialType.System_Int64,
                                        (SpecialType.System_UInt32, SpecialType.System_Int32) => SpecialType.System_Int64,
                                        (SpecialType.System_Int16, SpecialType.System_UInt32) => SpecialType.System_Int64,
                                        (SpecialType.System_UInt32, SpecialType.System_Int16) => SpecialType.System_Int64,
                                        (SpecialType.System_SByte, SpecialType.System_UInt32) => SpecialType.System_Int64,
                                        (SpecialType.System_UInt32, SpecialType.System_SByte) => SpecialType.System_Int64,

                                        // 4. Either side is a 64-bit Signed integer (long) -> Forces long
                                        (SpecialType.System_Int64, _) => SpecialType.System_Int64,
                                        (_, SpecialType.System_Int64) => SpecialType.System_Int64,

                                        // 5. Either side is a 64-bit Unsigned integer (ulong) -> Forces ulong
                                        (SpecialType.System_UInt64, _) => SpecialType.System_UInt64,
                                        (_, SpecialType.System_UInt64) => SpecialType.System_UInt64,

                                        // 6. Native Native-Sized Integers (nint / nuint)
                                        (SpecialType.System_IntPtr, _) => SpecialType.System_IntPtr,
                                        (_, SpecialType.System_IntPtr) => SpecialType.System_IntPtr,
                                        (SpecialType.System_UIntPtr, _) => SpecialType.System_UIntPtr,
                                        (_, SpecialType.System_UIntPtr) => SpecialType.System_UIntPtr,

                                        // 7. Floating-point types override integers entirely
                                        (SpecialType.System_Double, _) => SpecialType.System_Double,
                                        (_, SpecialType.System_Double) => SpecialType.System_Double,
                                        (SpecialType.System_Single, _) => SpecialType.System_Single,
                                        (_, SpecialType.System_Single) => SpecialType.System_Single,
                                        (SpecialType.System_Decimal, _) => SpecialType.System_Decimal,
                                        (_, SpecialType.System_Decimal) => SpecialType.System_Decimal,

                                        // 8. Fallback Default Rule
                                        // Handles (int - int), and all small integral types (char, short, ushort, byte, sbyte) 
                                        // which automatically undergo Unary Promotion to System_Int32.
                                        _ => SpecialType.System_Int32
                                    };
                                }
                                else if (node.IsKind(SyntaxKind.AddExpression))
                                {
                                    resultType = (lhsType.SpecialType, rhsType.SpecialType) switch
                                    {
                                        // 1. String Concatenation Rule (Unique to Addition)
                                        (SpecialType.System_String, _) => SpecialType.System_String,
                                        (_, SpecialType.System_String) => SpecialType.System_String,

                                        // 2. Both Unsigned 32-bit
                                        (SpecialType.System_UInt32, SpecialType.System_UInt32) => SpecialType.System_UInt32,

                                        // 3. Unsigned 32-bit mixed with smaller unsigned/char types -> Resolves to uint
                                        (SpecialType.System_UInt32, SpecialType.System_Char) => SpecialType.System_UInt32,
                                        (SpecialType.System_Char, SpecialType.System_UInt32) => SpecialType.System_UInt32,
                                        (SpecialType.System_UInt32, SpecialType.System_UInt16) => SpecialType.System_UInt32,
                                        (SpecialType.System_UInt16, SpecialType.System_UInt32) => SpecialType.System_UInt32,
                                        (SpecialType.System_UInt32, SpecialType.System_Byte) => SpecialType.System_UInt32,
                                        (SpecialType.System_Byte, SpecialType.System_UInt32) => SpecialType.System_UInt32,

                                        // 4. Mixed Sign 32-bit (int vs uint / uint vs int) -> Promotes to long
                                        (SpecialType.System_Int32, SpecialType.System_UInt32) => SpecialType.System_Int64,
                                        (SpecialType.System_UInt32, SpecialType.System_Int32) => SpecialType.System_Int64,
                                        (SpecialType.System_Int16, SpecialType.System_UInt32) => SpecialType.System_Int64,
                                        (SpecialType.System_UInt32, SpecialType.System_Int16) => SpecialType.System_Int64,
                                        (SpecialType.System_SByte, SpecialType.System_UInt32) => SpecialType.System_Int64,
                                        (SpecialType.System_UInt32, SpecialType.System_SByte) => SpecialType.System_Int64,

                                        // 5. Either side is a 64-bit Signed integer (long) -> Forces long
                                        (SpecialType.System_Int64, _) => SpecialType.System_Int64,
                                        (_, SpecialType.System_Int64) => SpecialType.System_Int64,

                                        // 6. Either side is a 64-bit Unsigned integer (ulong) -> Forces ulong
                                        (SpecialType.System_UInt64, _) => SpecialType.System_UInt64,
                                        (_, SpecialType.System_UInt64) => SpecialType.System_UInt64,

                                        // 7. Native Native-Sized Integers (nint / nuint)
                                        (SpecialType.System_IntPtr, _) => SpecialType.System_IntPtr,
                                        (_, SpecialType.System_IntPtr) => SpecialType.System_IntPtr,
                                        (SpecialType.System_UIntPtr, _) => SpecialType.System_UIntPtr,
                                        (_, SpecialType.System_UIntPtr) => SpecialType.System_UIntPtr,

                                        // 8. Floating-point types override integers entirely
                                        (SpecialType.System_Double, _) => SpecialType.System_Double,
                                        (_, SpecialType.System_Double) => SpecialType.System_Double,
                                        (SpecialType.System_Single, _) => SpecialType.System_Single,
                                        (_, SpecialType.System_Single) => SpecialType.System_Single,
                                        (SpecialType.System_Decimal, _) => SpecialType.System_Decimal,
                                        (_, SpecialType.System_Decimal) => SpecialType.System_Decimal,

                                        // 9. Fallback Default Rule
                                        _ => SpecialType.System_Int32
                                    };
                                }
                                else if (node.IsKind(SyntaxKind.MultiplyExpression))
                                {
                                    resultType = (lhsType.SpecialType, rhsType.SpecialType) switch
                                    {
                                        // 1. Both Unsigned 32-bit
                                        (SpecialType.System_UInt32, SpecialType.System_UInt32) => SpecialType.System_UInt32,

                                        // 2. Unsigned 32-bit mixed with smaller unsigned/char types -> Resolves to uint
                                        (SpecialType.System_UInt32, SpecialType.System_Char) => SpecialType.System_UInt32,
                                        (SpecialType.System_Char, SpecialType.System_UInt32) => SpecialType.System_UInt32,
                                        (SpecialType.System_UInt32, SpecialType.System_UInt16) => SpecialType.System_UInt32,
                                        (SpecialType.System_UInt16, SpecialType.System_UInt32) => SpecialType.System_UInt32,
                                        (SpecialType.System_UInt32, SpecialType.System_Byte) => SpecialType.System_UInt32,
                                        (SpecialType.System_Byte, SpecialType.System_UInt32) => SpecialType.System_UInt32,

                                        // 3. Mixed Sign 32-bit (int vs uint / uint vs int) -> Promotes to long
                                        (SpecialType.System_Int32, SpecialType.System_UInt32) => SpecialType.System_Int64,
                                        (SpecialType.System_UInt32, SpecialType.System_Int32) => SpecialType.System_Int64,
                                        (SpecialType.System_Int16, SpecialType.System_UInt32) => SpecialType.System_Int64,
                                        (SpecialType.System_UInt32, SpecialType.System_Int16) => SpecialType.System_Int64,
                                        (SpecialType.System_SByte, SpecialType.System_UInt32) => SpecialType.System_Int64,
                                        (SpecialType.System_UInt32, SpecialType.System_SByte) => SpecialType.System_Int64,

                                        // 4. Either side is a 64-bit Signed integer (long) -> Forces long
                                        (SpecialType.System_Int64, _) => SpecialType.System_Int64,
                                        (_, SpecialType.System_Int64) => SpecialType.System_Int64,

                                        // 5. Either side is a 64-bit Unsigned integer (ulong) -> Forces ulong
                                        (SpecialType.System_UInt64, _) => SpecialType.System_UInt64,
                                        (_, SpecialType.System_UInt64) => SpecialType.System_UInt64,

                                        // 6. Native Native-Sized Integers (nint / nuint)
                                        (SpecialType.System_IntPtr, _) => SpecialType.System_IntPtr,
                                        (_, SpecialType.System_IntPtr) => SpecialType.System_IntPtr,
                                        (SpecialType.System_UIntPtr, _) => SpecialType.System_UIntPtr,
                                        (_, SpecialType.System_UIntPtr) => SpecialType.System_UIntPtr,

                                        // 7. Floating-point types override integers entirely
                                        (SpecialType.System_Double, _) => SpecialType.System_Double,
                                        (_, SpecialType.System_Double) => SpecialType.System_Double,
                                        (SpecialType.System_Single, _) => SpecialType.System_Single,
                                        (_, SpecialType.System_Single) => SpecialType.System_Single,
                                        (SpecialType.System_Decimal, _) => SpecialType.System_Decimal,
                                        (_, SpecialType.System_Decimal) => SpecialType.System_Decimal,

                                        // 8. Fallback Default Rule
                                        _ => SpecialType.System_Int32
                                    };
                                }
                                else if (node.IsKind(SyntaxKind.DivideExpression))
                                {
                                    resultType = (lhsType.SpecialType, rhsType.SpecialType) switch
                                    {
                                        // 1. Both Unsigned 32-bit
                                        (SpecialType.System_UInt32, SpecialType.System_UInt32) => SpecialType.System_UInt32,

                                        // 2. Unsigned 32-bit mixed with smaller unsigned/char types -> Resolves to uint
                                        (SpecialType.System_UInt32, SpecialType.System_Char) => SpecialType.System_UInt32,
                                        (SpecialType.System_Char, SpecialType.System_UInt32) => SpecialType.System_UInt32,
                                        (SpecialType.System_UInt32, SpecialType.System_UInt16) => SpecialType.System_UInt32,
                                        (SpecialType.System_UInt16, SpecialType.System_UInt32) => SpecialType.System_UInt32,
                                        (SpecialType.System_UInt32, SpecialType.System_Byte) => SpecialType.System_UInt32,
                                        (SpecialType.System_Byte, SpecialType.System_UInt32) => SpecialType.System_UInt32,

                                        // 3. Mixed Sign 32-bit (int vs uint / uint vs int) -> Promotes to long
                                        (SpecialType.System_Int32, SpecialType.System_UInt32) => SpecialType.System_Int64,
                                        (SpecialType.System_UInt32, SpecialType.System_Int32) => SpecialType.System_Int64,
                                        (SpecialType.System_Int16, SpecialType.System_UInt32) => SpecialType.System_Int64,
                                        (SpecialType.System_UInt32, SpecialType.System_Int16) => SpecialType.System_Int64,
                                        (SpecialType.System_SByte, SpecialType.System_UInt32) => SpecialType.System_Int64,
                                        (SpecialType.System_UInt32, SpecialType.System_SByte) => SpecialType.System_Int64,

                                        // 4. Either side is a 64-bit Signed integer (long) -> Forces long
                                        (SpecialType.System_Int64, _) => SpecialType.System_Int64,
                                        (_, SpecialType.System_Int64) => SpecialType.System_Int64,

                                        // 5. Either side is a 64-bit Unsigned integer (ulong) -> Forces ulong
                                        (SpecialType.System_UInt64, _) => SpecialType.System_UInt64,
                                        (_, SpecialType.System_UInt64) => SpecialType.System_UInt64,

                                        // 6. Native Native-Sized Integers (nint / nuint)
                                        (SpecialType.System_IntPtr, _) => SpecialType.System_IntPtr,
                                        (_, SpecialType.System_IntPtr) => SpecialType.System_IntPtr,
                                        (SpecialType.System_UIntPtr, _) => SpecialType.System_UIntPtr,
                                        (_, SpecialType.System_UIntPtr) => SpecialType.System_UIntPtr,

                                        // 7. Floating-point types override integers entirely
                                        (SpecialType.System_Double, _) => SpecialType.System_Double,
                                        (_, SpecialType.System_Double) => SpecialType.System_Double,
                                        (SpecialType.System_Single, _) => SpecialType.System_Single,
                                        (_, SpecialType.System_Single) => SpecialType.System_Single,
                                        (SpecialType.System_Decimal, _) => SpecialType.System_Decimal,
                                        (_, SpecialType.System_Decimal) => SpecialType.System_Decimal,

                                        // 8. Fallback Default Rule
                                        _ => SpecialType.System_Int32
                                    };
                                }

                                bool isSignedResult = resultType switch
                                {
                                    // Unsigned integer types
                                    SpecialType.System_Byte => false,
                                    SpecialType.System_UInt16 => false,
                                    SpecialType.System_UInt32 => false,
                                    SpecialType.System_UInt64 => false,
                                    SpecialType.System_UIntPtr => false, // nuint
                                    SpecialType.System_Char => false, // Treats data as 16-bit unsigned

                                    // Signed integer and floating-point types
                                    SpecialType.System_SByte => true,
                                    SpecialType.System_Int16 => true,
                                    SpecialType.System_Int32 => true,
                                    SpecialType.System_Int64 => true,
                                    SpecialType.System_IntPtr => true,  // nint
                                    SpecialType.System_Single => true,  // float
                                    SpecialType.System_Double => true,  // double
                                    SpecialType.System_Decimal => true,  // decimal

                                    // Non-numeric types (objects, strings, booleans, void, etc.)
                                    _ => false
                                };

                                var leftRank = lhsType.GetNumericRangeRank();
                                var rightRank = rhsType.GetNumericRangeRank();
                                var int32Rank = visitor.Global.SystemInt32.GetNumericRangeRank();
                                //if both precision are less than int, no math operation on them can exceed int, no need to | 0
                                if (isSignedResult && leftRank < int32Rank && rightRank < int32Rank)
                                    return false;
                                if (!isChecked)
                                {
                                    visitor.CurrentTypeWriter.Write(node, "((");
                                    visitor.Visit(node.Left);
                                    visitor.CurrentTypeWriter.Write(node, " ");
                                    visitor.CurrentTypeWriter.Write(node, node.OperatorToken.Text);
                                    visitor.CurrentTypeWriter.Write(node, " ");
                                    visitor.Visit(node.Right);
                                    visitor.CurrentTypeWriter.Write(node, ")");
                                    if (isSignedResult)
                                    {
                                        visitor.CurrentTypeWriter.Write(node, " | 0)");
                                    }
                                    else
                                    {
                                        visitor.CurrentTypeWriter.Write(node, " >>> 0)");
                                    }
                                }
                                else
                                {
                                    visitor.CurrentTypeWriter.Write(node, visitor.Global.GlobalName);
                                    visitor.CurrentTypeWriter.Write(node, ".");
                                    visitor.CurrentTypeWriter.Write(node, Constants.IntegerChecked);
                                    visitor.CurrentTypeWriter.Write(node, "(");
                                    visitor.Visit(node.Left);
                                    visitor.CurrentTypeWriter.Write(node, " ");
                                    visitor.CurrentTypeWriter.Write(node, node.OperatorToken.Text);
                                    visitor.CurrentTypeWriter.Write(node, " ");
                                    visitor.Visit(node.Right);
                                    visitor.CurrentTypeWriter.Write(node, ", ");
                                    visitor.CurrentTypeWriter.Write(node, isSignedResult ? "1" : "0");
                                    visitor.CurrentTypeWriter.Write(node, ")");
                                }
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
