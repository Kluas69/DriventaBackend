namespace Driventa.Domain.Enums;

public enum LoadStatus
{
    Available = 0,
    Negotiating = 1,
    Booked = 2,
    Dispatched = 3,
    PickedUp = 4,
    InTransit = 5,
    Delivered = 6,
    Completed = 7,
    Cancelled = 8,
    Issue = 9
}