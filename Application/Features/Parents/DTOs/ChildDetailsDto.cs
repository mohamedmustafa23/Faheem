using Application.Features.Students.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Parents.DTOs
{
    public class ChildDetailsDto
    {
        public LinkedChildDto ChildInfo { get; set; } = new();
        public List<StudentGroupDto> Groups { get; set; } = new();
        public List<StudentTodaySessionDto> TodaySchedule { get; set; } = new();
    }
}
