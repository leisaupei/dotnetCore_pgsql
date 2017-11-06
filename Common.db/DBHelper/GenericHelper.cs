using System;
namespace Common.db.DBHelper
{
    public static class GenericHelper<T>{
        public static QueryHelper<T> Generic=>new QueryHelper<T>();
    }
}