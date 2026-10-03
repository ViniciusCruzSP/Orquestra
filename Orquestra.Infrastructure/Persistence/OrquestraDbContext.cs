using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Orquestra.Application.Abstractions;
using Orquestra.Application.Errors;
using Orquestra.Domain.Entities;
using Orquestra.Infrastructure.Identity;

namespace Orquestra.Infrastructure.Persistence;

public sealed class OrquestraDbContext(
    DbContextOptions<OrquestraDbContext> options,
    ICurrentUser currentUser) : IdentityDbContext<ApplicationUser>(options), IOrquestraDb
{
    private Guid CurrentOrganizationId => currentUser.OrganizationId;

    IQueryable<Organization> IOrquestraDb.Organizations => Set<Organization>();
    IQueryable<Department> IOrquestraDb.Departments => Set<Department>();
    IQueryable<Employee> IOrquestraDb.Employees => Set<Employee>();
    IQueryable<AssetCategory> IOrquestraDb.AssetCategories => Set<AssetCategory>();
    IQueryable<Asset> IOrquestraDb.Assets => Set<Asset>();
    IQueryable<Custody> IOrquestraDb.Custodies => Set<Custody>();
    IQueryable<CustodyItem> IOrquestraDb.CustodyItems => Set<CustodyItem>();
    IQueryable<AssetMovement> IOrquestraDb.AssetMovements => Set<AssetMovement>();
    IQueryable<OffboardingProcess> IOrquestraDb.Offboardings => Set<OffboardingProcess>();
    IQueryable<AuditLog> IOrquestraDb.AuditLogs => Set<AuditLog>();

    void IOrquestraDb.Add<T>(T entity) => Set<T>().Add(entity);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(b =>
        {
            b.Property(x => x.OrganizationId).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.HasIndex(x => new { x.OrganizationId, x.Id });
            b.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Organization>(b =>
        {
            b.ToTable("Organizations");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            b.HasIndex(x => x.Slug).IsUnique();
            b.HasQueryFilter(x => x.Id == CurrentOrganizationId);
        });

        ConfigureTenantEntity<Department>(modelBuilder, "Departments");
        modelBuilder.Entity<Department>(b =>
        {
            b.Property(x => x.Name).HasMaxLength(150).IsRequired();
            b.HasIndex(x => new { x.OrganizationId, x.Name })
                .IsUnique().HasFilter("[IsActive] = 1");
        });

        ConfigureTenantEntity<Employee>(modelBuilder, "Employees");
        modelBuilder.Entity<Employee>(b =>
        {
            b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            b.Property(x => x.Email).HasMaxLength(256).IsRequired();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
            b.Property(x => x.RowVersion).IsRowVersion();
            b.HasIndex(x => new { x.OrganizationId, x.Email })
                .IsUnique().HasFilter("[Status] <> 'Inactive'");
            b.HasOne<Department>().WithMany()
                .HasForeignKey(nameof(Employee.OrganizationId), nameof(Employee.DepartmentId))
                .HasPrincipalKey(nameof(Department.OrganizationId), nameof(Department.Id))
                .OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureTenantEntity<AssetCategory>(modelBuilder, "AssetCategories");
        modelBuilder.Entity<AssetCategory>(b =>
        {
            b.Property(x => x.Name).HasMaxLength(150).IsRequired();
            b.HasIndex(x => new { x.OrganizationId, x.Name })
                .IsUnique().HasFilter("[IsActive] = 1");
        });

        ConfigureTenantEntity<Asset>(modelBuilder, "Assets");
        modelBuilder.Entity<Asset>(b =>
        {
            b.Property(x => x.AssetTag).HasMaxLength(100).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Brand).HasMaxLength(120).IsRequired();
            b.Property(x => x.Model).HasMaxLength(120).IsRequired();
            b.Property(x => x.SerialNumber).HasMaxLength(150);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
            b.Property(x => x.RowVersion).IsRowVersion();
            b.HasIndex(x => new { x.OrganizationId, x.AssetTag }).IsUnique();
            b.HasOne<AssetCategory>().WithMany()
                .HasForeignKey(nameof(Asset.OrganizationId), nameof(Asset.CategoryId))
                .HasPrincipalKey(nameof(AssetCategory.OrganizationId), nameof(AssetCategory.Id))
                .OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureTenantEntity<Custody>(modelBuilder, "Custodies");
        modelBuilder.Entity<Custody>(b =>
        {
            b.Property(x => x.DeliveredByUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.HasIndex(x => new { x.OrganizationId, x.EmployeeId, x.DeliveredAt });
            b.HasOne<Employee>().WithMany()
                .HasForeignKey(nameof(Custody.OrganizationId), nameof(Custody.EmployeeId))
                .HasPrincipalKey(nameof(Employee.OrganizationId), nameof(Employee.Id))
                .OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureTenantEntity<CustodyItem>(modelBuilder, "CustodyItems");
        modelBuilder.Entity<CustodyItem>(b =>
        {
            b.Property(x => x.ReturnedByUserId).HasMaxLength(450);
            b.Property(x => x.ReturnNotes).HasMaxLength(2000);
            b.HasIndex(x => new { x.OrganizationId, x.AssetId })
                .IsUnique().HasFilter("[ReturnedAt] IS NULL");
            b.HasIndex(x => new { x.OrganizationId, x.CustodyId });
            b.HasOne<Custody>().WithMany()
                .HasForeignKey(nameof(CustodyItem.OrganizationId), nameof(CustodyItem.CustodyId))
                .HasPrincipalKey(nameof(Custody.OrganizationId), nameof(Custody.Id))
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Asset>().WithMany()
                .HasForeignKey(nameof(CustodyItem.OrganizationId), nameof(CustodyItem.AssetId))
                .HasPrincipalKey(nameof(Asset.OrganizationId), nameof(Asset.Id))
                .OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureTenantEntity<AssetMovement>(modelBuilder, "AssetMovements");
        modelBuilder.Entity<AssetMovement>(b =>
        {
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            b.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(24);
            b.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(24);
            b.Property(x => x.PerformedByUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.HasIndex(x => new { x.OrganizationId, x.AssetId, x.OccurredAt });
            b.HasOne<Asset>().WithMany()
                .HasForeignKey(nameof(AssetMovement.OrganizationId), nameof(AssetMovement.AssetId))
                .HasPrincipalKey(nameof(Asset.OrganizationId), nameof(Asset.Id))
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Custody>().WithMany()
                .HasForeignKey(nameof(AssetMovement.OrganizationId), nameof(AssetMovement.CustodyId))
                .HasPrincipalKey(nameof(Custody.OrganizationId), nameof(Custody.Id))
                .OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureTenantEntity<OffboardingProcess>(modelBuilder, "OffboardingProcesses");
        modelBuilder.Entity<OffboardingProcess>(b =>
        {
            b.Property(x => x.StartedByUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.CompletedByUserId).HasMaxLength(450);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.HasIndex(x => new { x.OrganizationId, x.EmployeeId })
                .IsUnique().HasFilter("[CompletedAt] IS NULL");
            b.HasOne<Employee>().WithMany()
                .HasForeignKey(nameof(OffboardingProcess.OrganizationId), nameof(OffboardingProcess.EmployeeId))
                .HasPrincipalKey(nameof(Employee.OrganizationId), nameof(Employee.Id))
                .OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureTenantEntity<AuditLog>(modelBuilder, "AuditLogs");
        modelBuilder.Entity<AuditLog>(b =>
        {
            b.Property(x => x.Action).HasMaxLength(100).IsRequired();
            b.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Details).HasMaxLength(2000);
            b.HasIndex(x => new { x.OrganizationId, x.OccurredAt });
        });
    }

    private void ConfigureTenantEntity<TEntity>(ModelBuilder modelBuilder, string table)
        where TEntity : class
    {
        var b = modelBuilder.Entity<TEntity>();
        b.ToTable(table);
        b.HasKey("Id");
        b.HasAlternateKey("OrganizationId", "Id");
        b.HasOne<Organization>().WithMany().HasForeignKey("OrganizationId")
            .OnDelete(DeleteBehavior.Restrict);

        // Build the filter from the mapped properties so each query uses this context's tenant.
        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(TEntity), "entity");
        var tenant = System.Linq.Expressions.Expression.Property(parameter, "OrganizationId");
        var current = System.Linq.Expressions.Expression.Property(
            System.Linq.Expressions.Expression.Constant(this), nameof(CurrentOrganizationId));
        var body = System.Linq.Expressions.Expression.Equal(tenant, current);
        var filter = System.Linq.Expressions.Expression.Lambda<Func<TEntity, bool>>(body, parameter);
        b.HasQueryFilter(filter);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not (Department or Employee or AssetCategory or Asset or Custody or
                CustodyItem or AssetMovement or OffboardingProcess or AuditLog)) continue;
            if (entry.State is EntityState.Unchanged or EntityState.Detached) continue;
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Os registros de negócio rastreados não podem ser excluídos fisicamente.");

            var tenant = currentUser.OrganizationId;
            var organizationId = (Guid)entry.Property("OrganizationId").CurrentValue!;
            if (tenant == Guid.Empty || organizationId != tenant ||
                (entry.State == EntityState.Modified &&
                 (Guid)entry.Property("OrganizationId").OriginalValue! != tenant))
                throw new UnauthorizedAccessException("O registro não pertence à organização atual.");
        }

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("O registro foi alterado enquanto você o editava. Recarregue os dados e tente novamente.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException("Já existe um registro com o mesmo valor único.");
        }
    }
}
