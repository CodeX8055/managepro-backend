using backend.Models;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public interface IOrganizationService { Task<List<OrganizationDto>> GetAllAsync(); Task<OrganizationDto?> CreateAsync(CreateOrganizationDto dto); }
public class OrganizationService : IOrganizationService {
    private readonly AppDbContext _db;
    public OrganizationService(AppDbContext db) => _db = db;
    public async Task<List<OrganizationDto>> GetAllAsync() => await _db.Organizations.Select(o => new OrganizationDto{ Id=o.Id, Name=o.Name, CreatedAt=o.CreatedAt }).ToListAsync();
    public async Task<OrganizationDto?> CreateAsync(CreateOrganizationDto dto) {
        var org = new Organization { Id=Guid.NewGuid(), Name=dto.Name };
        _db.Organizations.Add(org); await _db.SaveChangesAsync();
        return new OrganizationDto { Id=org.Id, Name=org.Name, CreatedAt=org.CreatedAt };
    }
}
