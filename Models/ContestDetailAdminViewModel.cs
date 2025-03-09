using System.Collections.Generic;

namespace JamesThewProject.Models.ViewModels
{
    public class ContestDetailAdminViewModel
    {
        public Contest? Contest { get; set; }
        public List<SubmissionRecipeViewModel> SubmissionRecipes { get; set; } = new List<SubmissionRecipeViewModel>();
    }
}
