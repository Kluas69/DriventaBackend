using Driventa.Application.Interfaces;
using Driventa.Domain.Entities;
using Driventa.Infrastructure.Persistence;

namespace Driventa.Infrastructure.Repositories;

public class BrokerRepository : BaseRepository<Broker>, IBrokerRepository
{
    public BrokerRepository(AppDbContext context) : base(context) { }
}