using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI_trust.Models;

[Table("surveys")]
public partial class Survey
{
    public int Id { get; set; }

    public string? Question { get; set; }

   

    public virtual ICollection<Useranswersurvey> Useranswersurveys { get; set; } = new List<Useranswersurvey>();
}
