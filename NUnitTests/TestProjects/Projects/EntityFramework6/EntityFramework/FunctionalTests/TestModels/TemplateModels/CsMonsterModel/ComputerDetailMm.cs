
//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


namespace Proj8b9180bea7178d8348de47e28237c05ddb8a8244.EntityFramework.FunctionalTests.TestModels.TemplateModels.CsMonsterModel
{

using System;
    using System.Collections.Generic;
    
public partial class ComputerDetailMm
{

    public ComputerDetailMm()
    {

        this.Dimensions = new DimensionsMm();

    }


    public int ComputerDetailId { get; set; }

    public string Manufacturer { get; set; }

    public string Model { get; set; }

    public string Serial { get; set; }

    public string Specifications { get; set; }

    public System.DateTime PurchaseDate { get; set; }



    public DimensionsMm Dimensions { get; set; }



    public virtual ComputerMm Computer { get; set; }

}

}