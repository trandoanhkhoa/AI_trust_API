using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI_trust.Models;


[Table("useranswersurveys")]
public partial class Useranswersurvey
{
    public int Id { get; set; }

    public int? Userid { get; set; }

    public int? Surveyid { get; set; }

    public int? Answer { get; set; }

    public virtual Survey? Survey { get; set; }

    public virtual User? User { get; set; }
}
