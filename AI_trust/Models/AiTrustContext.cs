using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AI_trust.Models;

public partial class AiTrustContext : DbContext
{
    public AiTrustContext()
    {
    }

    public AiTrustContext(DbContextOptions<AiTrustContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<Responseai> Responseais { get; set; }

    public virtual DbSet<Setting> Settings { get; set; }

    public virtual DbSet<Survey> Surveys { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Useranswer> Useranswers { get; set; }

    public virtual DbSet<Useranswersurvey> Useranswersurveys { get; set; }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)

    //    => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=AI_trust;Username=postgres;Password=120303;SSL Mode=Disable");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_questions");

            entity.ToTable("questions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Answer).HasColumnName("answer");
            entity.Property(e => e.Correctanswer)
                .HasMaxLength(10)
                .HasColumnName("correctanswer");
            entity.Property(e => e.Correctanswerdesc).HasColumnName("correctanswerdesc");
            entity.Property(e => e.Hallucination).HasColumnName("hallucination");
            entity.Property(e => e.Hallucinationanswer)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("hallucinationanswer");
            entity.Property(e => e.Image)
                .HasMaxLength(100)
                .HasColumnName("image");
            entity.Property(e => e.Question1).HasColumnName("question");
            entity.Property(e => e.Timetries).HasColumnName("timetries");
        });

        modelBuilder.Entity<Responseai>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_responseai");

            entity.ToTable("responseai");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Answerai).HasColumnName("answerai");
            entity.Property(e => e.Questionid).HasColumnName("questionid");
            entity.Property(e => e.Questionuser).HasColumnName("questionuser");
            entity.Property(e => e.Time)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("time");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.Question).WithMany(p => p.Responseais)
                .HasForeignKey(d => d.Questionid)
                .HasConstraintName("fk_responseai_questions");

            entity.HasOne(d => d.User).WithMany(p => p.Responseais)
                .HasForeignKey(d => d.Userid)
                .HasConstraintName("fk_responseai_users");
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_settings");

            entity.ToTable("settings");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Shufflequestion).HasColumnName("shufflequestion");
            entity.Property(e => e.Time).HasColumnName("time");
            entity.Property(e => e.Timelimit).HasColumnName("timelimit");
        });

        modelBuilder.Entity<Survey>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_surveys");

            entity.ToTable("surveys");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Question)
                .HasMaxLength(250)
                .HasColumnName("question");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_users");

            entity.ToTable("users");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Dotest)
                .HasDefaultValue(false)
                .HasColumnName("dotest");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .HasColumnName("email");
            entity.Property(e => e.Gender)
                .HasMaxLength(20)
                .HasColumnName("gender");
            entity.Property(e => e.Gpa)
                .HasPrecision(18, 4)
                .HasColumnName("gpa");
            entity.Property(e => e.Major)
                .HasMaxLength(50)
                .HasColumnName("major");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Password)
                .HasMaxLength(50)
                .HasColumnName("password");
            entity.Property(e => e.Role)
                .HasMaxLength(10)
                .HasColumnName("role");
            entity.Property(e => e.StudyYear)
                .HasMaxLength(50)
                .HasColumnName("study_year");
            entity.Property(e => e.Typeoftest).HasColumnName("typeoftest");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
            entity.Property(e => e.Yearofbirth).HasColumnName("yearofbirth");
        });

        modelBuilder.Entity<Useranswer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_useranswers");

            entity.ToTable("useranswers");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Questionid).HasColumnName("questionid");
            entity.Property(e => e.Startedat)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("startedat");
            entity.Property(e => e.Submittedat)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("submittedat");
            entity.Property(e => e.Trytimes).HasColumnName("trytimes");
            entity.Property(e => e.Useranswer1)
                .HasMaxLength(5)
                .HasColumnName("useranswer");
            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.Usetime).HasColumnName("usetime");

            entity.HasOne(d => d.Question).WithMany(p => p.Useranswers)
                .HasForeignKey(d => d.Questionid)
                .HasConstraintName("fk_useranswers_questions");

            entity.HasOne(d => d.User).WithMany(p => p.Useranswers)
                .HasForeignKey(d => d.Userid)
                .HasConstraintName("fk_useranswers_users");
        });

        modelBuilder.Entity<Useranswersurvey>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_useranswersurvey");

            entity.ToTable("useranswersurvey");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Answer).HasColumnName("answer");
            entity.Property(e => e.Surveyid).HasColumnName("surveyid");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.Survey).WithMany(p => p.Useranswersurveys)
                .HasForeignKey(d => d.Surveyid)
                .HasConstraintName("fk_useranswersurvey_surveys");

            entity.HasOne(d => d.User).WithMany(p => p.Useranswersurveys)
                .HasForeignKey(d => d.Userid)
                .HasConstraintName("fk_useranswersurvey_users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
