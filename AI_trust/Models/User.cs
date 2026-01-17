using System;
using System.Collections.Generic;

namespace AI_trust.Models;

public partial class User
{
    public int Id { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? Email { get; set; }

    public string? Name { get; set; }

    public string? Role { get; set; }

    public string? Gender { get; set; }

    public string? Major { get; set; }

    public string? StudyYear { get; set; }

    public bool? Dotest { get; set; }

    public int? Yearofbirth { get; set; }

    public decimal? Gpa { get; set; }

    public int? Typeoftest { get; set; }

    public virtual ICollection<Responseai> Responseais { get; set; } = new List<Responseai>();

    public virtual ICollection<Useranswer> Useranswers { get; set; } = new List<Useranswer>();

    public virtual ICollection<Useranswersurvey> Useranswersurveys { get; set; } = new List<Useranswersurvey>();
}
