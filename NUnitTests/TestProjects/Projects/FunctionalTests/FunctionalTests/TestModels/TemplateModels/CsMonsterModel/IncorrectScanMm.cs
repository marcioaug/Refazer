
//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


namespace Proj8d452499b23e250232406fa9c875973a054b17f9.FunctionalTests.TestModels.TemplateModels.CsMonsterModel
{

using System;
    using System.Collections.Generic;
    
public partial class IncorrectScanMm
{

    public int IncorrectScanId { get; set; }

    public byte[] ExpectedCode { get; set; }

    public byte[] ActualCode { get; set; }

    public System.DateTime ScanDate { get; set; }

    public string Details { get; set; }



    public virtual BarcodeMm ExpectedBarcode { get; set; }

    public virtual BarcodeMm ActualBarcode { get; set; }

}

}
