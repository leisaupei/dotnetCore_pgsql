using Meta.Driver.DbHelper;
using Meta.Driver.Interface;
using Meta.Driver.SqlBuilder;
using Meta.Driver.SqlBuilder.AnalysisExpression;
using Meta.xUnitTest.DAL;
using Meta.xUnitTest.Model;
using Meta.xUnitTest.Options;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.Ordering;

namespace Meta.xUnitTest
{
	[Order(3)]
	public class SelectExpression : BaseTest
	{
		public SelectExpression(ITestOutputHelper output) : base(output)
		{
		}

		[Fact]
		public void OrderBy()
		{
			var list = People.Select.OrderByDescending(a => a.Create_time).ToList();
		}
		[Fact]
		public void Join()
		{
			var union = Student.Select
					.InnerJoin<ClassmateModel>((a, b) => a.Id == b.Student_id)
					.ToOne();

			Assert.Null(union);
		}
		[Fact]
		public void WhereStaticMember()
		{
			var info = Student.Select
					.Where(a => a.People_id == StuPeopleId1)
					.ToOne();

			Assert.Equal(info.People_id, StuPeopleId1);
		}
		[Fact]
		public void WhereInternalMember()
		{
			var id = StuPeopleId1;
			var info = Student.Select
					.Where(a => a.People_id == id)
					.ToOne();

			Assert.Equal(info.People_id, StuPeopleId1);
		}
		[Fact]
		public void WhereExists()
		{
			var info = Student.Select.WhereExists(People.Select.Field(b => b.Id).Where(b => b.Id == StuPeopleId1)).ToOne();
		}
		[Fact]
		public void WhereClassProperty()
		{
			var model = new TestModel
			{
				Id = StuPeopleId1
			};
			var info = Student.Select
					.Where(a => a.People_id == model.Id)
					.ToOne();

			Assert.Equal(StuPeopleId1, info.People_id);
		}
		[Fact]
		public void WhereNull()
		{
			var info = People.Select
					.Where(a => a.Sex == null)
					.ToOne();

			Assert.Null(info?.Sex);
		}
		[Fact]
		public void WhereDefault()
		{
			var info = People.Select
					.Where(a => a.Id == default)
					.ToOne();

			Assert.Null(info?.Sex);
		}
		[Fact]
		public void WhereInnerClassPeoperty()
		{
			var model = new ParentTestModel
			{
				Info = new TestModel
				{
					Id = StuPeopleId1
				}
			};
			var model1 = new ParentPreantTestModel
			{
				Info = new ParentTestModel
				{
					Info = new TestModel
					{
						Id = StuPeopleId1
					}
				}
			};
			var info = Student.Select
					 .Where(a => a.People_id == model.Info.Id)
					 .ToOne();
			info = Student.Select
					 .Where(a => a.People_id == model1.Info.Info.Id)
					 .ToOne();
			Assert.Equal(info.People_id, StuPeopleId1);
		}
		[Fact]
		public void WhereClassStaticMember()
		{
			var info = Student.Select
				  .Where(a => a.People_id == TestModel._id)
				  .ToOne();
			Assert.Equal(info.People_id, StuPeopleId1);
		}
		[Fact]
		public void WhereConst()
		{
			var info = People.Select
				.Where(a => a.Id == Guid.Empty)
				.ToOne();
			info = People.Select
				.Where(a => a.Name == "leisaupei")
				.ToOne();
			Assert.Equal("leisaupei", info.Name);
		}
		[Fact]
		public void WhereMethodLast()
		{
			var info = People.Select
				  .Where(a => a.Name == "leisaupei".ToString())
				  .ToOne();
			Assert.Equal("leisaupei", info.Name);
		}
		[Fact]
		public void WhereDifficultMethod()
		{
			var info = People.Select
				  .Where(a => a.Create_time < DateTime.Now.AddDays(-1))
				  .ToOne();
			Assert.Equal("leisaupei", info.Name);
		}
		[Fact]
		public void WhereMethod()
		{
			var info = People.Select
				  .Where(a => a.Id == Guid.Parse(StuPeopleId1.ToString()))
				  .ToOne();
			Assert.Equal(info.Id, StuPeopleId1);
		}
		[Fact]
		public void WhereNewArray()
		{
			var info = TypeTest.Select
				.Where(a => a.Array_type == new[] { 1 })
				.ToOne();
			var info1 = TypeTest.Select
				.Where(a => a.Uuid_array_type == new[] { Guid.Empty })
				.ToOne();
			if (info != null)
				Assert.Equal(1, info.Array_type.FirstOrDefault());
		}

		[Fact]
		public void WhereNewClass()
		{
			var info = People.Select
				  .Where(a => a.Id == new TestModel()
				  {
					  Id = StuPeopleId1
				  }.Id)
				  .ToOne();
			Assert.Equal(StuPeopleId1, info.Id);
		}
		[Fact]
		public void WhereNewStruct()
		{
			var info = People.Select
				  .Where(a => a.Id == new Guid())
				  .ToOne();
			Assert.True(info == null || info?.Id == new Guid());
		}
		[Fact]
		public void WhereMultiple()
		{
			var info = People.Select
				  .Where(a => (a.Id == new Guid() && a.Id == StuPeopleId1) || a.Id == StuPeopleId2)
				  .ToOne();
			Assert.Equal(StuPeopleId2, info.Id);
		}
		[Fact]
		public void UnionMultiple()
		{
			var info = People.Select
				.InnerJoin<StudentModel>((a, b) => a.Id == b.People_id && (b.People_id == StuPeopleId1 || a.Id == StuPeopleId2))
				.Where<StudentModel>(b => b.People_id == StuPeopleId1)
				.ToOne();
			Assert.Equal(StuPeopleId1, info.Id);
		}
		[Fact]
		public void WhereOperationExpression()
		{
			var info = People.Select.Where(a => DateTime.Today - a.Create_time > TimeSpan.FromDays(2)).ToOne();
			Assert.NotNull(info);
		}
		[Fact]
		public void WhereIndexParameter()
		{
			Guid[] id = new[] { StuPeopleId1, Guid.Empty };
			var info = People.Select.Where(a => a.Id == id[0]).ToOne();
			Assert.NotNull(info);
		}
		[Fact]
		public void WhereFieldParameter()
		{
			var arr = new[] { 1 };
			var info = TypeTest.Select.Where(a => a.Array_type[1] == arr[0]).ToOne();
			Assert.NotNull(info);
		}
		[Fact]
		public void WhereFieldLength()
		{
			var info = TypeTest.Select.Where(a => a.Array_type.Length == 2).ToOne();
			Assert.NotNull(info);
		}
		[Fact]
		public void WhereEnum()
		{
			EDataState value = EDataState.正常;
			var info = TypeTest.Select.Where(a => a.Enum_type == value).ToOne();
			info = TypeTest.Select.Where(a => a.Enum_type == EDataState.正常).ToOne();
			info = TypeTest.Select.Where(a => a.Int4_type == (int)value).ToOne();
			info = TypeTest.Select.Where(a => a.Int4_type == (int)EDataState.正常).ToOne();
			Assert.NotNull(info);
		}
		[Fact]
		public void WhereContains()
		{
			var xx = new PeopleModel();
			xx.Id = Guid.Empty;
			var b = new PeopleModel();
			b.Id = Guid.Empty;
			var c = TypeTest.Select.Where(a => new[] { xx.Id, b.Id }.Contains(a.Id)).ToOne();
			TypeTestModel info = null;
			info = TypeTest.Select.Where(a => new[] { 1, 3, 4 }.Contains(a.Int4_type.Value)).ToOne();
			////a.int_type <> all(array[2,3])
			info = TypeTest.Select.Where(a => !a.Array_type.Contains(3)).ToOne();
			////3 = any(a.array_type)
			info = TypeTest.Select.Where(a => a.Array_type.Contains(3)).ToOne();
			////var ints = new int[] { 2, 3 }.Select(f => f).ToList();
			////a.int_type = any(array[2,3])
			info = TypeTest.Select.Where(a => new int[] { 2, 3 }.Select(f => f).ToArray().Contains(a.Int4_type.Value)).ToOne();
			info = TypeTest.Select.Where(a => new[] { (int)EDataState.已删除, (int)EDataState.正常 }.Contains(a.Int4_type.Value)).ToOne();

			Assert.NotNull(info);
		}
		[Fact]
		public void WhereStringLike()
		{
			TypeTestModel info = null;


			//'xxx' like a.Varchar_type || '%'
			info = TypeTest.Select.Where(a => "xxxxxxxxxxxx".StartsWith(a.Varchar_type)).ToOne();
			Assert.NotNull(info);
			//a.varchar_type like '%xxx%'
			info = TypeTest.Select.Where(a => a.Varchar_type.Contains("xxx")).ToOne();
			Assert.NotNull(info);
			//a.varchar_type like 'xxx%'
			info = TypeTest.Select.Where(a => a.Varchar_type.StartsWith("xxx")).ToOne();
			Assert.NotNull(info);
			//a.varchar_type like '%xxx'
			info = TypeTest.Select.Where(a => a.Varchar_type.EndsWith("xxx")).ToOne();
			Assert.NotNull(info);

			//a.varchar_type ilike '%xxx%'
			info = TypeTest.Select.Where(a => a.Varchar_type.Contains("xxx", StringComparison.OrdinalIgnoreCase)).ToOne();
			Assert.NotNull(info);
			//a.varchar_type ilike 'xxx%'
			info = TypeTest.Select.Where(a => a.Varchar_type.StartsWith("xxx", StringComparison.OrdinalIgnoreCase)).ToOne();
			Assert.NotNull(info);
			//a.varchar_type ilike '%xxx'
			info = TypeTest.Select.Where(a => a.Varchar_type.EndsWith("xxx", StringComparison.OrdinalIgnoreCase)).ToOne();
			Assert.NotNull(info);
		}
		[Fact]
		public void WhereToString()
		{
			TypeTestModel info = null;
			//a.varchar_type::text = 'xxxx'
			info = TypeTest.Select.Where(a => a.Varchar_type.ToString() == "xxxx").ToOne();
			Assert.NotNull(info);
			ParentPreantTestModel model = new ParentPreantTestModel { Name = "xxxx" };
			info = TypeTest.Select.Where(a => a.Varchar_type.ToString() == model.Name.ToString()).ToOne();
			Assert.NotNull(info);

		}
		[Fact]
		public void WhereEqualsFunction()
		{
			TypeTestModel info = null;
			Func<string, string> fuc = (str) =>
			{
				return "xxxx";
			};
			info = TypeTest.Select.Where(a => a.Varchar_type == fuc("xxxx")).ToOne();
			Assert.NotNull(info);
		}
		[Fact]
		public void WhereBlock()
		{
			TypeTestModel info = null;
			var judge = 0;
			info = TypeTest.Select.Where(a => a.Varchar_type == (judge == 0 ? "xxxx" : "")).ToOne();
			Assert.NotNull(info);
		}
		[Fact]
		public void WhereArrayEqual()
		{
			TypeTestModel info = null;
			info = TypeTest.Select.Where(a => a.Array_type == new[] { 0, 1 }).ToOne();
			info = TypeTest.Select.Where(a => a.Uuid_array_type == new[] { Guid.Empty }).ToOne();
			info = TypeTest.Select.Where(a => new[] { "广东" } == a.Varchar_array_type).ToOne();
			info = TypeTest.Select.Where(a => a.Varchar_array_type == new[] { "广东,广州" }).ToOne();
			Assert.NotNull(info);
		}
		[Fact]
		public void WhereCoalesce()
		{
			TypeTestModel info = null;
			var sum = TypeTest.Select.Sum(a => a.Int8_type ?? 0, 0);
			info = TypeTest.Select.Where(a => (a.Int4_type ?? 3) == 3).ToOne();
			Assert.NotNull(info);

		}
		[Fact]
		public void WhereEqualFieldWithNamespace()
		{
			var info = Student.Select
					.Where(a => a.People_id == Meta.xUnitTest.BaseTest.StuPeopleId1)
					.ToOne();

			Assert.Equal(info.People_id, Meta.xUnitTest.BaseTest.StuPeopleId1);
		}
		[Fact]
		public void WhereCompareSelf()
		{
			var info = Student.Select
					.Where(a => a.People_id == a.Id || a.People_id == StuPeopleId1)
					.ToOne();

			Assert.Equal(info.People_id, Meta.xUnitTest.BaseTest.StuPeopleId1);
		}
		[Fact]
		public void WhereIn()
		{
			var info = Student.Select
					.WhereIn(a => a.People_id, Student.Select.Field(a => a.People_id).Where(a => a.People_id == StuPeopleId1))
					.ToOne();

			Assert.Equal(info.People_id, Meta.xUnitTest.BaseTest.StuPeopleId1);
		}
		[Fact]
		public void Test()
		{
			//	ParentPreantTestModel model = new ParentPreantTestModel { Name = "xxx" };
			//	var info = TypeTest.Select.Where(a => !a.Varchar_type.Contains(model.Name, StringComparison.OrdinalIgnoreCase)).ToOne();
			//Expression<Func<ClassmateModel, Guid>> expression = f => f.Grade_id;
			//MemberExpression body = expression.Body as MemberExpression;
			//var equ = Expression.Equal(body, Expression.Constant(Guid.Empty, typeof(Guid)));
			//var lambda = Expression.Lambda<Func<ClassmateModel, bool>>(equ, body.Expression as ParameterExpression);
			//SqlExpressionVisitor.Instance.VisitCondition(lambda);

		}
		public class ParentPreantTestModel
		{
			public string Name { get; set; }
			public ParentTestModel Info { get; set; }
		}
		public class ParentTestModel
		{
			public TestModel Info { get; set; }
		}
		public class TestModel
		{
			public static Guid _id = StuPeopleId1;
			public Guid Id { get; set; }
		}
	}
}
