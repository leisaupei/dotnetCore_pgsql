using Meta.Common.DbHelper;
using Meta.Common.Interface;
using Meta.Common.SqlBuilder;
using Meta.xUnitTest.DAL;
using Meta.xUnitTest.Extensions;
using Meta.xUnitTest.Model;
using Meta.xUnitTest.Options;
using Newtonsoft.Json.Linq;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.Ordering;

namespace Meta.xUnitTest
{
    [Order(2)]
    public class SelectNormal : BaseTest
    {
        public SelectNormal(ITestOutputHelper output) : base(output)
        {
        }

        [Fact, Order(1)]
        public void ToOne()
        {
            var info = Student.Select.Where(a => a.People_id == StuPeopleId1).ToOne();

            Assert.Equal(StuPeopleId1, info.People_id);
        }

        [Fact, Order(2)]
        public void GetItem()
        {
            var info = People.GetItem(StuPeopleId1);
            //Assert.Equal(StuPeopleId1, info.Id);
        }
        [Fact, Order(3), Description("return a List<T>")]
        public void GetItems()
        {
            var info = Student.GetItemsByPeople_id(new[] { StuPeopleId1, StuPeopleId2 });

            Assert.IsType<List<StudentModel>>(info);
            Assert.Contains(info, f => f.People_id == StuPeopleId1);
            Assert.Contains(info, f => f.People_id == StuPeopleId2);
        }

        [Fact, Order(4)]
        public void ToOneT()
        {
            var peopleId = Student.Select.Where(a => a.People_id == StuPeopleId1).ToOne<Guid>("people_id");
            var info = People.Select.Where(a => a.Id == StuPeopleId1).ToOne<ToOneTTestModel>("name,id");

            var emptyNullablePeopleId = Student.Select.Where(f => f.People_id == Guid.Empty).ToOne<Guid?>("people_id");
            var emptyPeopleId = Student.Select.Where(f => f.People_id == Guid.Empty).ToOne<Guid>("people_id");

            Assert.IsType<ToOneTTestModel>(info);
            Assert.Equal(StuPeopleId1, info.Id);
            Assert.Equal(StuPeopleId1, peopleId);
            Assert.Null(emptyNullablePeopleId);
            Assert.Equal(Guid.Empty, emptyPeopleId);
        }

        [Fact, Order(5)]
        public void ToOneTuple()
        {
            var info = People.Select.Where(a => a.Id == StuPeopleId1).ToOne<(Guid id, string name)>("id,name");
            // if not found
            var notFoundInfo = People.Select.Where(a => a.Id == Guid.Empty).ToOne<(Guid id, string name)>("id,name");
            var notFoundNullableInfo = People.Select.Where(a => a.Id == Guid.Empty).ToOne<(Guid? id, string name)>("id,name");


            Assert.Equal(StuPeopleId1, info.id);
            Assert.Equal(Guid.Empty, notFoundInfo.id);
            Assert.Null(notFoundNullableInfo.id);
        }

        [Fact, Order(6)]
        public void GetItemByUniqueKey()
        {
            var info = Student.GetItemByStu_no(StuNo1);

            Assert.Equal(StuNo1, info.Stu_no);
        }

        [Fact, Order(7)]
        public void GetItemsByUniqueKey()
        {
            var info = Student.GetItemsByStu_no(new[] { StuNo1, StuNo2 });

            Assert.Contains(info, f => f.Stu_no == StuNo1);
            Assert.Contains(info, f => f.Stu_no == StuNo2);
        }

        [Fact, Order(8)]
        public void ToOneDictonary()
        {
            // all
            var info = People.Select.Where(a => a.Id == StuPeopleId1).ToOne<Dictionary<string, object>>();
            // option
            var info1 = People.Select.Where(a => a.Id == StuPeopleId1).ToOne<Dictionary<string, object>>("name,id");
            Assert.Equal(StuPeopleId1, Guid.Parse(info["id"].ToString()));
            Assert.Equal(StuPeopleId1, Guid.Parse(info1["id"].ToString()));
        }

        [Fact, Order(9)]
        public void ToList()
        {
            var info = People.Select.WhereAny(a => a.Id, new[] { StuPeopleId1, StuPeopleId2 }).ToList();

            Assert.Contains(info, f => f.Id == StuPeopleId1);
            Assert.Contains(info, f => f.Id == StuPeopleId2);
        }

        [Fact, Order(10)]
        public void ToListTuple()
        {
            var info = People.Select.WhereAny(a => a.Id, new[] { StuPeopleId1, StuPeopleId2 }).ToList<(Guid, string)>("id,name");

            Assert.Contains(info, f => f.Item1 == StuPeopleId1);
            Assert.Contains(info, f => f.Item1 == StuPeopleId2);
        }

        [Fact, Order(11)]
        public void ToListDictonary()
        {
            // all
            var info = People.Select.WhereAny(a => a.Id, new[] { StuPeopleId1, StuPeopleId2 }).ToList<Dictionary<string, object>>();
            // option
            var info1 = People.Select.WhereAny(a => a.Id, new[] { StuPeopleId1, StuPeopleId2 }).ToList<Dictionary<string, object>>("name,id");

            Assert.Contains(info, f => f["id"].ToString() == StuPeopleId1.ToString());
            Assert.Contains(info, f => f["id"].ToString() == StuPeopleId2.ToString());
            Assert.Contains(info1, f => f["id"].ToString() == StuPeopleId1.ToString());
            Assert.Contains(info1, f => f["id"].ToString() == StuPeopleId2.ToString());
        }

        [Fact, Order(12)]
        public void ToPipe()
        {
            object[] obj = PgsqlHelper.ExecuteDataReaderPipe(new ISqlBuilder[] {
                People.Select.WhereAny(a => a.Id, new[] { StuPeopleId1, StuPeopleId2 }).ToListPipe(),
                People.Select.Where(a => a.Id == StuPeopleId1).ToOnePipe<(Guid, string)>("id,name"),
                People.Select.Where(a =>a.Id==StuPeopleId1).ToListPipe<(Guid, string)>("id,name"),
                People.Select.WhereAny(a => a.Id, new[] { StuPeopleId1, StuPeopleId2 }).ToListPipe<Dictionary<string, object>>("name,id"),
                People.Select.Where(a => a.Id == StuPeopleId1).ToOnePipe<ToOneTTestModel>("name,id"),
                Student.Select.Where(a => a.People_id == StuPeopleId1).ToOnePipe<Guid>("people_id"),
                Student.Select.LeftJoin<PeopleModel>((a,b) => a.People_id == b.Id,true).Where(a => a.People_id == Guid.Empty).ToOneUnionPipe<PeopleModel>(), // if not found
				People.Update(StuPeopleId1).Set(a => a.Age, 40).ToRowsPipe()
                 });
            var info = obj[0].ToObjectArray().OfType<PeopleModel>();
            var info1 = ((Guid, string))obj[1];
            var info2 = obj[2].ToObjectArray().OfType<(Guid, string)>();
            var info3 = obj[3].ToObjectArray().OfType<Dictionary<string, object>>();
            var info4 = (ToOneTTestModel)obj[4];
            var info5 = (Guid)obj[5];
            var info6 = ((StudentModel, PeopleModel))obj[6];
            var info7 = obj[7];
            Assert.Contains(info, f => f.Id == StuPeopleId1);
            Assert.Equal(StuPeopleId1, info1.Item1);
            Assert.Contains(info2, f => f.Item1 == StuPeopleId1);
            Assert.Contains(info3, f => f["id"].ToString() == StuPeopleId1.ToString());
            Assert.Equal(StuPeopleId1, info4.Id);
            Assert.Equal(StuPeopleId1, info5);
            //Assert.Null(info6);
            Assert.True(info7 is int);
        }
        [Fact, Order(13)]
        public void Count()
        {
            Stopwatch stop = new Stopwatch();
            stop.Start();
            var count = People.Select.Count();
            for (int i = 0; i < 1000; i++)
            {

                count = People.Select.Count();
            }
            stop.Stop();
            _output.WriteLine(stop.ElapsedMilliseconds.ToString());
            Assert.True(count >= 0);
        }
        [Fact, Order(14)]
        public void Max()
        {
            var maxAge = People.Select.Max(a => a.Age);

            maxAge = Student.Select.InnerJoin<PeopleModel>((a, b) => a.People_id == b.Id).Max<PeopleModel, int>(b => b.Age);
            Assert.True(maxAge >= 0);
        }
        [Fact, Order(15)]
        public void Min()
        {
            var minAge = People.Select.Min(a => a.Age);
            Assert.True(minAge >= 0);
        }
        [Fact, Order(16), Description("the type of T must be same as the column's type")]
        public void Avg()
        {
            var avgAge = People.Select.Avg<decimal>(a => a.Age);
            Assert.True(avgAge >= 0);
        }

        [Fact, Order(17), Description("same usage as ToOne<T>(), but T is ValueType")]
        public void ToScalar()
        {
            var id = People.Select.Where(a => a.Id == StuPeopleId1).ToScalar<Guid>("id");
            Assert.Equal(StuPeopleId1, id);
        }
        [Fact, Order(18)]
        public void OrderBy()
        {
            //var infos = People.Select.InnerJoin<StudentModel>((a, b) => a.Id == b.People_id)
            //	.OrderByDescing(f => f.Age).ToList();
        }
        [Fact, Order(19)]
        public void GroupBy()
        {

        }
        [Fact, Order(20)]
        public void Page()
        {

        }
        [Fact, Order(21)]
        public void Limit()
        {

        }
        [Fact, Order(22)]
        public void Skip()
        {

        }
        [Fact, Order(23), Description("using with group by expression")]
        public void Having()
        {

        }
        [Fact, Order(24), Description("")]
        public void ToUnion()
        {
            Stopwatch stop = new Stopwatch();
            stop.Start();
            var union0 = Student.Select.InnerJoin<PeopleModel>((a, b) => a.People_id == b.Id && a.Stu_no == StuNo1, true).ToOneUnion<PeopleModel>();

            var a = stop.ElapsedMilliseconds;
            var union1 = Student.Select.InnerJoin<PeopleModel>((a, b) => a.People_id == b.Id && a.Stu_no == StuNo1, true).ToOneUnion<PeopleModel>();
            var b = stop.ElapsedMilliseconds;
            var union2 = Student.Select.InnerJoin<PeopleModel>((a, b) => a.People_id == b.Id && a.Stu_no == StuNo1, true).ToOneUnion<PeopleModel>();
            var c = stop.ElapsedMilliseconds;
            stop.Stop();

            _output.WriteLine(string.Concat(a, ",", b, ",", c));
        }
        [Fact, Order(25), Description("")]
        public void ToUnionCompare()
        {
            Stopwatch stop = new Stopwatch();
            stop.Start();
            var union0 = Student.Select.InnerJoin<PeopleModel>((a, b) => a.People_id == b.Id && a.Stu_no == StuNo1).ToOne();
            var a = stop.ElapsedMilliseconds;
            var union1 = Student.Select.InnerJoin<PeopleModel>((a, b) => a.People_id == b.Id && a.Stu_no == StuNo1).ToOne();
            var b = stop.ElapsedMilliseconds;
            var union2 = Student.Select.InnerJoin<PeopleModel>((a, b) => a.People_id == b.Id && a.Stu_no == StuNo1).ToOne();
            var c = stop.ElapsedMilliseconds;
            stop.Stop();

            _output.WriteLine(string.Concat(a, ",", b, ",", c));
        }
        [Fact, Order(16), Description("the type of T must be same as the column's type")]
        public void Sum()
        {
            var avgAge = People.Select.Sum<long>(a => a.Age);
            Assert.True(avgAge >= 0);
        }
    }
}
