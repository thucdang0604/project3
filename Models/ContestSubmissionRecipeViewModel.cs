using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace JamesThewProject.Models
{
    public class ContestSubmissionRecipeViewModel
    {
        [BindNever]
        public Contest Contest { get; set; } = new Contest();

        public CreateOrEditRecipeViewModel RecipeViewModel { get; set; } = new CreateOrEditRecipeViewModel();
    }
}
