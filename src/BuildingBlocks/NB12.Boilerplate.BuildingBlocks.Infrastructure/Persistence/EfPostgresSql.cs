using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Persistence
{
    public static class EfPostgresSql
    {
        public static (string QualifiedTable, StoreObjectIdentifier StoreId, IEntityType EntityType) Table<TEntity>(DbContext db)
        {
            var et = db.Model.FindEntityType(typeof(TEntity))
                ?? throw new InvalidOperationException($"Entity type '{typeof(TEntity).Name}' is not mapped.");

            var table = et.GetTableName()
                ?? throw new InvalidOperationException($"Entity type '{typeof(TEntity).Name}' has no table name.");

            var schema = et.GetSchema();
            var storeId = StoreObjectIdentifier.Table(table, schema);

            var qualified = schema is null
                ? $"{Q(table)}"
                : $"{Q(schema)}.{Q(table)}";

            return (qualified, storeId, et);
        }

        public static bool HasProperty(IEntityType et, string propertyName)
            => et.FindProperty(propertyName) is not null;

        public static string Column(IEntityType et, StoreObjectIdentifier storeId, string propertyName)
        {
            var prop = et.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"Property '{propertyName}' not found on '{et.ClrType.Name}'.");

            var col = prop.GetColumnName(storeId)
                ?? throw new InvalidOperationException($"Column name not resolved for '{et.ClrType.Name}.{propertyName}'.");

            return Q(col);
        }

        public static string Q(string ident)
            => "\"" + ident.Replace("\"", "\"\"") + "\"";
    }
}
