
//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


namespace Proj326d525b1d7d8e1c95f7227be63238ec783a9e92.EntityFramework.FunctionalTests.TestModels.TemplateModels.CsMonsterModel
{

using System;
    using System.Collections.Generic;
    
public partial class PageViewMm
{

    public int PageViewId { get; set; }

    public string Username { get; set; }

    public System.DateTime Viewed { get; set; }

    public string PageUrl { get; set; }



    public virtual Another.Place.LoginMm Login { get; set; }

}

}