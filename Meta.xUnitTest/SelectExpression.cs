using Meta.Common.DbHelper;
using Meta.Common.Interface;
using Meta.Common.SqlBuilder;
using Meta.xUnitTest.DAL;
using Meta.xUnitTest.Model;
using Meta.xUnitTest.Options;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xunit;
using Xunit.Extensions.Ordering;

namespace Meta.xUnitTest
{
    [Order(3)]
    public class SelectExpression : BaseTest
    {
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
        public void WhereClassProperty()
        {
            var model = new TestModel();
            model.Id = StuPeopleId1;
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
        public void WhereInnerClassPeoperty()
        {
            var model = new ParentTestModel();
            model.Info = new TestModel();
            model.Info.Id = StuPeopleId1;
            var model1 = new ParentPreantTestModel();
            model1.Info = new ParentTestModel();
            model1.Info.Info = new TestModel();
            model1.Info.Info.Id = StuPeopleId1;
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
            Guid id = Guid.Empty;
            var info = People.Select.Where(a => DateTime.Today - a.Create_time > TimeSpan.FromDays(2)).ToOne();
            Assert.NotNull(info);
        }
        [Fact]
        public void Test()
        {
            var sql = "select *from people where id <> any( @pp)";
            var list = PgsqlHelper.ExecuteDataReaderList<PeopleModel>(sql, System.Data.CommandType.Text, new[] { new NpgsqlParameter("pp", new[] { StuPeopleId1, StuPeopleId2 }) });
            Assert.Contains(list, f => f.Id == StuPeopleId1);
        }
        public class ParentPreantTestModel
        {
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
