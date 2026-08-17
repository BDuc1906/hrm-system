namespace API.Messages.Events;

/// <summary>
/// Event published to RabbitMQ (via MassTransit) when an employee's monthly
/// attendance is closed/locked. Consumed by the Payroll Service to trigger
/// salary calculation.
/// </summary>
public class AttendanceMonthlyClosedEvent
{
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalWorkingDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateDays { get; set; }
    public int OnLeaveDays { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public DateTime LockedAt { get; set; }
}
