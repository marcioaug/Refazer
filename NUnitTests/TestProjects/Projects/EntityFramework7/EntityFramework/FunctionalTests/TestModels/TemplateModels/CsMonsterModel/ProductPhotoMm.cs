
//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


namespace Proj829dec5d15c3d930815d72ce2d6909c51a97b1ef.EntityFramework.FunctionalTests.TestModels.TemplateModels.CsMonsterModel
{

using System;
    using System.Collections.Generic;
    
public partial class ProductPhotoMm
{

    public ProductPhotoMm()
    {

        this.Features = new HashSet<ProductWebFeatureMm>();

    }


    public int ProductId { get; set; }

    public int PhotoId { get; set; }

    public byte[] Photo { get; set; }



    public virtual ICollection<ProductWebFeatureMm> Features { get; set; }

}

}