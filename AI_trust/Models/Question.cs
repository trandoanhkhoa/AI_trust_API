using System;
using System.Collections.Generic;

namespace AI_trust.Models;

public partial class Question
{
    public int Id { get; set; }

    public string? Question1 { get; set; }

    public string? Answer { get; set; }

    public string? Correctanswer { get; set; }

    public string? Correctanswerdesc { get; set; }

    public string? Image { get; set; }

    public string? Hallucination { get; set; }

    public string? Hallucinationanswer { get; set; }

    public int? Timetries { get; set; }

    public virtual ICollection<Responseai> Responseais { get; set; } = new List<Responseai>();

    public virtual ICollection<Useranswer> Useranswers { get; set; } = new List<Useranswer>();
}
