using System;
using Autofac.Features.Indexed;

namespace NosAyudamos
{
    class WorkflowFactory : IWorkflowFactory
    {
        readonly IIndex<Workflow, Func<IWorkflow>> workflows;

        public WorkflowFactory(IIndex<Workflow, Func<IWorkflow>> workflows) => this.workflows = workflows;

        public IWorkflow Create(Workflow workflow) => workflows[workflow]();
    }

    interface IWorkflowFactory
    {
        IWorkflow Create(Workflow workflow);
    }
}
