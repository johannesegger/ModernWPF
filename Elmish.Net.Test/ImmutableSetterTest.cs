﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using Xunit;

namespace Elmish.Net.Test
{
    public class ImmutableSetterTest
    {
        [Fact]
        public void ShouldCreatePropertySetter()
        {
            Expression<Func<A, string>> expr = o => o.B.C.Value;
            var setter = expr.CreateImmutableSetter();
            var a = new A(new B(new C("0"), new D("0"), new C[0]));
            var result = setter(a, "1");
            result.B.C.Value.Should().Be("1");
        }

        [Fact]
        public void ShouldNotTouchExistingProperties()
        {
            Expression<Func<A, string>> expr = o => o.B.C.Value;
            var setter = expr.CreateImmutableSetter();
            var a = new A(new B(new C("0"), new D("0"), new C[0]));
            var result = setter(a, "1");
            result.B.D.Should().BeSameAs(a.B.D);
        }

        public static IEnumerable<object[]> CollectionExpressionTestData
        {
            get
            {
                yield return new[]
                {
                    (Expression<Func<A, string>>)(o => o.B.CList[1].Value)
                };

                // Compiler infers the following as `o => o.B.CList.get_Item(1).Value`
                // Expression<Func<A, string>> expr = o => o.B.CList[1].Value;
                var parameter = Expression.Parameter(typeof(A), "o");
                yield return new[]
                {
                    Expression.Lambda<Func<A, string>>(
                        Expression.Property(
                            Expression.MakeIndex(
                                Expression.Property(
                                    Expression.Property(parameter, typeof(A).GetProperty(nameof(A.B))),
                                    typeof(B).GetProperty(nameof(B.CList))),
                                typeof(IReadOnlyList<C>).GetProperty("Item"),
                                new[] { Expression.Constant(1) }),
                            typeof(C).GetProperty(nameof(C.Value))),
                        parameter)
                };
            }
        }

        [Theory]
        [MemberData(nameof(CollectionExpressionTestData))]
        public void ShouldWorkWithCollections(Expression<Func<A, string>> expr)
        {
            var setter = expr.CreateImmutableSetter();
            var a = new A(new B(new C("0"), new D("0"), new [] { new C("0"), new C("1"), new C("2") }));
            var result = setter(a, "11");
            result.B.CList.Select(c => c.Value).Should().Equal("0", "11", "2");
        }

        [Fact]
        public void ShouldWorkWithSingleInternalConstructor()
        {
            Expression<Func<TypeWithSingleInternalConstructor, string>> expr = o => o.Value;
            var setter = expr.CreateImmutableSetter();
            var obj = new TypeWithSingleInternalConstructor("1");
            var result = setter(obj, "2");
            result.Value.Should().Be("2");
        }

        [Fact]
        public void ShouldChoosePublicConstructorOverInternalConstructor()
        {
            Expression<Func<TypeWithPublicAndInternalConstructor, string>> expr = o => o.Value;
            var setter = expr.CreateImmutableSetter();
            var obj = new TypeWithPublicAndInternalConstructor("1");
            var result = setter(obj, "2");
            result.Value.Should().Be("2");
        }

        public class A
        {
            public A(B b)
            {
                B = b;
            }

            public B B { get; }
        }

        public class B
        {
            public B(C c, D d, IEnumerable<C> cList)
            {
                C = c;
                D = d;
                CList = cList.ToImmutableList();
            }

            public C C { get; }
            public D D { get; }
            public ImmutableList<C> CList { get; }
        }

        public class C
        {
            public C(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        public class D
        {
            public D(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        public class TypeWithSingleInternalConstructor
        {
            internal TypeWithSingleInternalConstructor(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        public class TypeWithPublicAndInternalConstructor
        {
            internal TypeWithPublicAndInternalConstructor(string value, int dummy)
            {
                Value = value;
            }

            public TypeWithPublicAndInternalConstructor(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }
    }
}
