
//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


namespace Projed9469e53a99d7f7f46176906582f04423619183.EntityFramework.FunctionalTests.TestModels.TemplateModels.CsMonsterModel
{

using System;
    using System.Collections.Generic;
    
public partial class OrderLineMm
{

    public OrderLineMm()
    {

        this.Quantity = 1;

    }


    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public string ConcurrencyToken { get; set; }



    public virtual OrderMm Order { get; set; }

    public virtual ProductMm Product { get; set; }

}

}