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

        public IWorkflow Select(Workflow workflow)
        {
            return workflows.First(w =>
                w.Value.GetType().GetTypeInfo().GetCustomAttribute<WorkflowAttribute>()?.Name == workflow.ToString()).Value;
        }
    }

    interface IWorkflowSelector
    {
        IWorkflow Select(Workflow workflow);
    }
}
