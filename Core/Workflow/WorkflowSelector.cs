using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NosAyudamos
{
    class WorkflowSelector : IWorkflowSelector
    {
        readonly IEnumerable<Lazy<IWorkflow>> workflows;

        public WorkflowSelector(IEnumerable<Lazy<IWorkflow>> workflows) => this.workflows = workflows;

        public IWorkflow Select(Role? role) => workflows
            .First(w => w.Value.GetType().GetCustomAttribute<WorkflowAttribute>()?.Name == role?.ToString()).Value;
    }

    interface IWorkflowSelector
    {
        IWorkflow Select(Role? role);
    }
}
