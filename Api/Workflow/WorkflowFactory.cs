using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NosAyudamos
{
    class WorkflowFactory : IWorkflowFactory
    {
        readonly IEnumerable<IWorkflow> workflows;
        public WorkflowFactory(IEnumerable<IWorkflow> workflows) => this.workflows = workflows;

        public IWorkflow Create(Workflow workflow)
        {
            return workflows.First(w =>
                w.GetType().GetTypeInfo().GetCustomAttribute<WorkflowAttribute>()?.Name == workflow.ToString());
        }
    }

    interface IWorkflowFactory
    {
        IWorkflow Create(Workflow workflow);
    }
}
