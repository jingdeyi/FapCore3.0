﻿using Fap.Core.DataAccess;
using Fap.Core.DataAccess.Interceptor;
using Fap.Core.Exceptions;
using Fap.Core.Extensions;
using Fap.Core.Infrastructure.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fap.Core.Infrastructure.Interceptor
{
    public class FapColumnDataInterceptor : DataInterceptorBase
    {
        private readonly IDbMetadataContext _metadataContext;
        public FapColumnDataInterceptor(IServiceProvider provider, IDbContext dbContext, IDbMetadataContext metadataContext) : base(provider, dbContext)
        {
            _metadataContext = metadataContext;
        }
        private FapColumn ToFapColumn(FapDynamicObject fapDynamicData) =>
        new FapColumn
        {

            TableName = fapDynamicData.Get(nameof(FapColumn.TableName)).ToString(),
            ColName = fapDynamicData.Get(nameof(FapColumn.ColName)).ToString(),
            ColType = fapDynamicData.Get(nameof(FapColumn.ColType)).ToString(),
            ColComment = fapDynamicData.Get(nameof(FapColumn.ColComment)).ToString(),
            ColLength = fapDynamicData.Get(nameof(FapColumn.ColLength)).ToString().ToInt(),
            ColPrecision = fapDynamicData.Get(nameof(FapColumn.ColPrecision)).ToString().ToInt(),
            IsMultiLang = fapDynamicData.Get(nameof(FapColumn.IsMultiLang)).ToString().ToInt()

        };

        public override void AfterDynamicObjectInsert(FapDynamicObject fapDynamicData)
        {
            string tableName = fapDynamicData.Get(nameof(FapColumn.TableName)).ToString();
            var table = _dbContext.QueryFirstOrDefault<FapTable>("select * from FapTable where TableName=@TableName", new Dapper.DynamicParameters(new { TableName = tableName }));
            if (table.IsSync == 1)
            {
                try
                {
                    _metadataContext.AddColumn(ToFapColumn(fapDynamicData));
                }
                catch (Exception)
                {
                    throw new FapException("物理表增加列失败！");
                }
            }

        }
        public override void BeforeDynamicObjectUpdate(FapDynamicObject fapDynamicData)
        {
            FapColumn newColumn = ToFapColumn(fapDynamicData);
            string fid = fapDynamicData.Get(nameof(FapColumn.Fid)).ToString();
            FapColumn oriColumn = _dbContext.Get<FapColumn>(fid);
            try
            {
                if (!newColumn.ColName.EqualsWithIgnoreCase(oriColumn.ColName))
                {
                    _metadataContext.RenameColumn(newColumn, oriColumn.ColName);
                }
                else if (!newColumn.ColType.EqualsWithIgnoreCase(oriColumn.ColType) || newColumn.ColLength != oriColumn.ColLength || newColumn.ColPrecision != oriColumn.ColPrecision)
                {
                    _metadataContext.AlterColumn(newColumn);
                }
                if (oriColumn.IsMultiLang == 0 && newColumn.IsMultiLang == 1)
                {
                    //新增多语字段
                    _metadataContext.AddMultiLangColumn(newColumn);
                }
                if (oriColumn.IsMultiLang == 1 && newColumn.IsMultiLang == 0)
                {
                    _metadataContext.DropMultiLangColumn(newColumn);
                }

            }
            catch (Exception)
            {
                throw new FapException("物理表修改列失败！");
            }
        }
        public override void BeforeDynamicObjectDelete(FapDynamicObject fapDynamicData)
        {
            string fid = fapDynamicData.Get("Fid").ToString();
            FapColumn column = _dbContext.Get<FapColumn>(fid);
            var table = _dbContext.QueryFirstOrDefault<FapTable>("select * from FapTable where TableName=@TableName", new Dapper.DynamicParameters(new { TableName = column.TableName }));
            if (table.IsSync == 1)
            {
                try
                {
                    _metadataContext.DropColumn(column);
                }
                catch (Exception)
                {
                    throw new FapException("物理表删除列失败！");
                }
            }
        }
       
    }
}
