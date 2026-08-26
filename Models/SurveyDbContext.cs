using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SurveyFormApp.Models;

public partial class SurveyDbContext : DbContext
{
    public SurveyDbContext()
    {
    }

    public SurveyDbContext(DbContextOptions<SurveyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<SurveyAnswer> SurveyAnswers { get; set; }

    public virtual DbSet<SurveyAnswerGroup> SurveyAnswerGroups { get; set; }

    public virtual DbSet<SurveyAssignment> SurveyAssignments { get; set; }

    public virtual DbSet<SurveyAttachment> SurveyAttachments { get; set; }

    public virtual DbSet<SurveyForm> SurveyForms { get; set; }

    public virtual DbSet<SurveyFormVersion> SurveyFormVersions { get; set; }

    public virtual DbSet<SurveyFraudFlag> SurveyFraudFlags { get; set; }

    public virtual DbSet<SurveyGeoValidation> SurveyGeoValidations { get; set; }

    public virtual DbSet<SurveyLocationLog> SurveyLocationLogs { get; set; }

    public virtual DbSet<SurveyQuestion> SurveyQuestions { get; set; }

    public virtual DbSet<SurveyQuestionGroup> SurveyQuestionGroups { get; set; }

    public virtual DbSet<SurveyQuestionOption> SurveyQuestionOptions { get; set; }

    public virtual DbSet<SurveyQuestionRule> SurveyQuestionRules { get; set; }

    public virtual DbSet<SurveyResponse> SurveyResponses { get; set; }

    public virtual DbSet<SurveyScore> SurveyScores { get; set; }

    public virtual DbSet<SurveySection> SurveySections { get; set; }

protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    if (!optionsBuilder.IsConfigured)
    {
        optionsBuilder.UseSqlServer("Name=ConnectionStrings:SurveyDb");
    }
}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SurveyAnswer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SurveyAn__3214EC07A2952798");

            entity.ToTable("SurveyAnswer");

            entity.HasIndex(e => e.QuestionId, "IX_Answer_Question");

            entity.HasIndex(e => e.ResponseId, "IX_Answer_Response");

            entity.HasIndex(e => e.ResponseId, "IX_Answer_ResponseId");

            entity.HasIndex(e => new { e.ResponseId, e.QuestionId }, "IX_Answer_Response_Question");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ValueNumber).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.AnswerGroup).WithMany(p => p.SurveyAnswers)
                .HasForeignKey(d => d.AnswerGroupId)
                .HasConstraintName("FK_Answer_Group");

            entity.HasOne(d => d.Question).WithMany(p => p.SurveyAnswers)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Answer_Question");

            entity.HasOne(d => d.Response).WithMany(p => p.SurveyAnswers)
                .HasForeignKey(d => d.ResponseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Answer_Response");
        });

        modelBuilder.Entity<SurveyAnswerGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SurveyAn__3214EC0718B8EF46");

            entity.ToTable("SurveyAnswerGroup");

            entity.HasIndex(e => e.ResponseId, "IX_AnswerGroup_Response");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Group).WithMany(p => p.SurveyAnswerGroups)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AnswerGroup_Group");

            entity.HasOne(d => d.Response).WithMany(p => p.SurveyAnswerGroups)
                .HasForeignKey(d => d.ResponseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AnswerGroup_Response");
        });

        modelBuilder.Entity<SurveyAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SurveyAs__3214EC07B42B3072");

            entity.ToTable("SurveyAssignment");

            entity.HasIndex(e => e.Status, "IX_Assignment_Status");

            entity.HasIndex(e => e.SurveyorId, "IX_Assignment_Surveyor");

            entity.Property(e => e.ApplicationId).HasMaxLength(100);
            entity.Property(e => e.BranchId).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF__SurveyAss__Creat__1EA48E88");
            entity.Property(e => e.Status)
                .HasMaxLength(60)
                .HasDefaultValue("ASSIGNED", "DF_SurveyAssignment_Status");
            entity.Property(e => e.SurveyorId).HasMaxLength(100);
            entity.Property(e => e.TakenBySurveyorId).HasMaxLength(100);

            entity.HasOne(d => d.FormVersion).WithMany(p => p.SurveyAssignments)
                .HasForeignKey(d => d.FormVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Assignment_FormVersion");
        });

        modelBuilder.Entity<SurveyAttachment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SurveyAt__3214EC074626BAE9");

            entity.ToTable("SurveyAttachment");

            entity.HasIndex(e => e.ResponseId, "IX_Attachment_Response");

            entity.Property(e => e.Checksum).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.FileName).HasMaxLength(400);
            entity.Property(e => e.FileType).HasMaxLength(100);
            entity.Property(e => e.FileUrl).HasMaxLength(1000);
            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(10, 7)");

            entity.HasOne(d => d.Question).WithMany(p => p.SurveyAttachments)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("FK_Attachment_Question");

            entity.HasOne(d => d.Response).WithMany(p => p.SurveyAttachments)
                .HasForeignKey(d => d.ResponseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attachment_Response");
        });

        modelBuilder.Entity<SurveyForm>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SurveyFo__3214EC072A72F4C9");

            entity.ToTable("SurveyForm");

            entity.HasIndex(e => e.FormCode, "IX_SurveyForm_FormCode").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.FormCode).HasMaxLength(100);
            entity.Property(e => e.FormName).HasMaxLength(400);
            entity.Property(e => e.ProductType).HasMaxLength(100);
        });

        modelBuilder.Entity<SurveyFormVersion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SurveyFo__3214EC07EF19CD11");

            entity.ToTable("SurveyFormVersion");

            entity.HasIndex(e => new { e.FormId, e.VersionNo }, "IX_FormVersion_Form_Version").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Form).WithMany(p => p.SurveyFormVersions)
                .HasForeignKey(d => d.FormId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FormVersion_Form");
        });

        modelBuilder.Entity<SurveyFraudFlag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SurveyFr__3214EC07B0648A40");

            entity.ToTable("SurveyFraudFlag");

            entity.HasIndex(e => e.ResponseId, "IX_Fraud_Response");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.FlagCode).HasMaxLength(100);

            entity.HasOne(d => d.Response).WithMany(p => p.SurveyFraudFlags)
                .HasForeignKey(d => d.ResponseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Fraud_Response");
        });

        modelBuilder.Entity<SurveyGeoValidation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SurveyGe__3214EC076C11182A");

            entity.ToTable("SurveyGeoValidation");

            entity.HasIndex(e => e.ResponseId, "IX_Geo_Response").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DebtorLatitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.DebtorLongitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.DistanceMeters).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SurveyLatitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.SurveyLongitude).HasColumnType("decimal(10, 7)");

            entity.HasOne(d => d.Response).WithOne(p => p.SurveyGeoValidation)
                .HasForeignKey<SurveyGeoValidation>(d => d.ResponseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Geo_Response");
        });

        modelBuilder.Entity<SurveyLocationLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SurveyLo__3214EC072D915062");

            entity.ToTable("SurveyLocationLog");

            entity.HasIndex(e => e.ResponseId, "IX_LocationLog_Response");

            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(10, 7)");

            entity.HasOne(d => d.Response).WithMany(p => p.SurveyLocationLogs)
                .HasForeignKey(d => d.ResponseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LocationLog_Response");
        });

        modelBuilder.Entity<SurveyQuestion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SurveyQu__3214EC076F8E720C");

            entity.ToTable("SurveyQuestion");

            entity.HasIndex(e => e.QuestionCode, "IX_Question_Code").IsUnique();

            entity.HasIndex(e => e.GroupId, "IX_Question_Group");

            entity.HasIndex(e => e.SectionId, "IX_Question_Section");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DefaultValue).HasMaxLength(200);
            entity.Property(e => e.HelpText).HasMaxLength(1000);
            entity.Property(e => e.InputMask).HasMaxLength(100);
            entity.Property(e => e.MaxValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MinValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Placeholder).HasMaxLength(400);
            entity.Property(e => e.QuestionCode).HasMaxLength(100);
            entity.Property(e => e.QuestionText).HasMaxLength(1000);
            entity.Property(e => e.QuestionType).HasMaxLength(60);
            entity.Property(e => e.UnitLabel).HasMaxLength(50);
            entity.Property(e => e.ValidationRegex).HasMaxLength(400);

            entity.HasOne(d => d.Group).WithMany(p => p.SurveyQuestions)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("FK_Question_Group");

            entity.HasOne(d => d.Section).WithMany(p => p.SurveyQuestions)
                .HasForeignKey(d => d.SectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Question_Section");
        });

        modelBuilder.Entity<SurveyQuestionGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SurveyQu__3214EC07B7360C94");

            entity.ToTable("SurveyQuestionGroup");

            entity.HasIndex(e => e.SectionId, "IX_Group_Section");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.GroupCode).HasMaxLength(100);
            entity.Property(e => e.GroupLabel).HasMaxLength(400);

            entity.HasOne(d => d.Section).WithMany(p => p.SurveyQuestionGroups)
                .HasForeignKey(d => d.SectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Group_Section");
        });

        modelBuilder.Entity<SurveyQuestionOption>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SurveyQu__3214EC07F2971262");

            entity.ToTable("SurveyQuestionOption");

            entity.HasIndex(e => e.QuestionId, "IX_Option_Question");

            entity.HasIndex(e => new { e.QuestionId, e.OptionValue }, "IX_Option_Question_Value").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.OptionLabel).HasMaxLength(400);
            entity.Property(e => e.OptionValue).HasMaxLength(200);

            entity.HasOne(d => d.Question).WithMany(p => p.SurveyQuestionOptions)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Option_Question");
        });

        modelBuilder.Entity<SurveyQuestionRule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SurveyQu__3214EC07FB503B4C");

            entity.ToTable("SurveyQuestionRule");

            entity.Property(e => e.Action).HasMaxLength(40);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Operator).HasMaxLength(40);
            entity.Property(e => e.Value).HasMaxLength(400);

            entity.HasOne(d => d.DependsOnQuestion).WithMany(p => p.SurveyQuestionRuleDependsOnQuestions)
                .HasForeignKey(d => d.DependsOnQuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rule_Depends");

            entity.HasOne(d => d.Question).WithMany(p => p.SurveyQuestionRuleQuestions)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rule_Question");
        });

        modelBuilder.Entity<SurveyResponse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SurveyRe__3214EC0771566256");

            entity.ToTable("SurveyResponse");

            entity.HasIndex(e => e.AssignmentId, "IX_Response_Assignment");

            entity.HasIndex(e => e.AssignmentId, "IX_Response_AssignmentId");

            entity.HasIndex(e => e.SyncId, "IX_Response_SyncId")
                .IsUnique()
                .HasFilter("([SyncId] IS NOT NULL)");

            entity.Property(e => e.AppVersion).HasMaxLength(40);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DeviceId).HasMaxLength(200);
            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Status).HasMaxLength(60);
            entity.Property(e => e.SurveyorId).HasMaxLength(100);
            entity.Property(e => e.SyncId).HasMaxLength(100);

            entity.HasOne(d => d.Assignment).WithMany(p => p.SurveyResponses)
                .HasForeignKey(d => d.AssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Response_Assignment");
        });

        modelBuilder.Entity<SurveyScore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SurveySc__3214EC07EF93A71D");

            entity.ToTable("SurveyScore");

            entity.HasIndex(e => e.ResponseId, "IX_Score_Response").IsUnique();

            entity.Property(e => e.ScoreEnvironment).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ScoreHousing).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ScoreIncome).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ScoreTotal).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Response).WithOne(p => p.SurveyScore)
                .HasForeignKey<SurveyScore>(d => d.ResponseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Score_Response");
        });

        modelBuilder.Entity<SurveySection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SurveySe__3214EC070C8308DB");

            entity.ToTable("SurveySection");

            entity.HasIndex(e => e.FormVersionId, "IX_Section_FormVersion");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SectionCode).HasMaxLength(100);
            entity.Property(e => e.SectionTitle).HasMaxLength(400);

            entity.HasOne(d => d.FormVersion).WithMany(p => p.SurveySections)
                .HasForeignKey(d => d.FormVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Section_FormVersion");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
