using System;
using System.Collections.Generic;

namespace AI_trust.Models;

public partial class Survey
{
    public int Id { get; set; }

    public string? Question { get; set; }

    public virtual ICollection<Useranswersurvey> Useranswersurveys { get; set; } = new List<Useranswersurvey>();
}
