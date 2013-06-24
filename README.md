Joe.Map
=======

What is Joe.Map? Joe.Map is quite simply object mapping software. It is similar in function to AutoMapper and Dozer. Joe.Map is attribute based ie you can set up your maps via attributes on your View Model/Data Transfer Object. A mapping would look something like this: `[ViewMapping("Name")]`.

###Why Map to View Models?

Mapping to View Models Provides a level of separation between your Model/Internal Objects and your UI/External Objects. You can create a View Model that might be a subset of your model or maybe it flattens your model. Lets look at some examples.

###Subset of Model

```
public class Blog
{
    public int ID { get; set; }
    [Required]
    public String Name { get; set; }
    [Column(TypeName = "ntext")]
    public String Entry { get; set; }
    public DateTime DateEntered { get; set; }
    public DateTime DateUpdate { get; set; }
    public DateTime? DatePublished { get; set; }
    public String Owner { get; set; }
    public Boolean Draft { get; set; }
    public virtual List<Comment> Comments { get; set; }
    public virtual List<BlogImage> Images { get; set; }
}
public class BlogListView
{
     [ViewMapping(ColumnPropertyName="ID", Key = true)]
     public int ID { get; set; }
     [Required]
     [ViewMapping(ColumnPropertyName="Name", Key = true)]
     public String Name { get; set; }
}
```
###Flatting of Model
```
public class Comment
{
     public int ID { get; set; }
     [Required]
     [Range(1, 9999999999999, ErrorMessage = "A Blog is required for this comment")]
     public int BlogID { get; set; }
     public virtual Blog Blog { get; set; }
     public String Owner { get; set; }
     public String Text { get; set; }
     public DateTime DateEntered { get; set; }
}
public class CommentView
{
     [ViewMapping(ColumnPropertyName = "ID", Key = true)]
     public int ID { get; set; }
     [ViewMapping(ColumnPropertyName = "Comment")]
     public String Comment {get;set;}
     [ViewMapping(ColumnPropertyName = "Blog.Name")]
     public String BlogName {get;set;}
}
```
###API

When preforming a map you can either map from a single model or from a list of model objects. Lets look at an example.
```
public class Repository<TModel, TViewModel>
{
   private IDBSet<TModel> _source;

   public Repository()
   {
     _source = new Context().Set<TModel>();
   }

   public IQueryable<TViewModel> GetListofViewModels()
   {
       return _source.Map<TModel, TViewModel>();
   }

   //Note you must include Joe.MapBack for this funtionality
   public TViewModel MapBack(TViewModel viewModel)
   {
      return _source.MapBack<TModel, TViewModel>(viewModel);
   }
}
```
In the above example we have create a repository object that takes in the Generics of `TModel` and `TViewModel` (for info on generics see: Generics in C#). `TModel` is the Source Object and `TViewModel` is the Destination Object. In GetListofViewModels() we are calling the `IQueryable` extension method `MapDBView<TModel, TViewModel>`(this `IQueryable<TModel> modelList`), this will generate an Expression Tree that maps the properties from the Model to the View Model.

###What is an Expression Tree?

Basically it is an object representation of a lambda style function. The Tree will be compiled and executed during run-time. Here is an example of a compiled expression tree.
```
//This is the An actual expression generated for the Comments section of a Blog Entry.
comment => new CommentView() 
{
  BlogID = comment.BlogID, 
  DateEntered = comment.DateEntered, 
  ID = comment.ID, 
  Owner = comment.Owner, 
  Text = comment.Text
}
```
Looks just like what you would write if you were doing the mapping manually! Using expression is much quicker and more efficient than reflection. On list of objects it allows you to Maintain the `IQuerable<T>` proxy wrapper. Why is this important? The answer is actually quite simple but sometimes confusing part of linq, simply put the query doesn't execute until you try to use the list. This comes in handy if your business or UI layer filters the data further. Now only the required data is fetched from the Database.

##The ViewMapping Attribute
```
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ViewMappingAttribute : Attribute
{
    public ViewMappingAttribute();

    //This can also be set via the constructor
    public string ColumnPropertyName { get; set; }
    public bool CreateNew { get; set; }
    public string GroupBy { get; set; }
    public bool Include { get; set; }
    public bool Key { get; set; }
    public int MaxDepth { get; set; }
    public bool OrderBy { get; set; }
    public bool Descending { get; set; }
    public int OrderBySequence { get; set; }
    public bool ReadOnly { get; set; }
    public bool WriteOnly { get; set; }
    public string Type { get; set; }
    public bool UseParentListForRelationships { get; set; }
    public string Where { get; set; }
    public String ToBoolean { get; set; }
    public String LinqFunction { get; set; }
}
```

**ColumnPropertyName** - This is how you set up the Mapping. Please note the mapper will parse properties on '.' so you can flatten your view model by access child object properties directly e.g. `"Blog.Name"`. If you want to select a Property within a list you man use a '-', it would look something like this: `"BlogList-Name"`. This would return you a list of string Blog names. You can also select list within list within list to infinity. Each list must contain the '-' separator e.g. `BlogList-Comments-SubComments-SubComments`. This will utilize the SelectMany Linq function to flatten the returned list to just a list of comments (It would be returning a list of all comments two levels deep). Another note, if the property is named the same you do not need to set this, the mapper will pick that up automatically e.g. Name will map to Name. Also you can pick up simple nested properties by combining their names e.g. if you want to access Blog.Name you could name your property `public string BlogName {get;set;}`.

**CreateNew** - Deals with mapping back your View Model to your Model. If a nested object within your View Model or properties that map to a nested object within your Model have a value and its corresponding Model Object is null, the mapper will Create a new Object for your model and add it to Object Tracking.

**GroupBy** - This is for list only. It will automatically group your results for you by the property you specify. You must set your ViewModel property type to: IGrouping<Key, IEnumerable<NestedViewModel> Blogs {get;set;}.

**Include** - Calls the linq method Include<T, TProperty>() for that specific View Model property.

**Key** - Specifies if the View Property is part of the Key for that object. This must be set in order to map back to Model object. This lets the Mapper fetch the Model that needs to be mapped back to automatically.

**MaxDepth** - This allows for the ability to have Recursive Views. Say you have a recursive list of comments (comments that have sub comments that have sub comments etc.) You might set up your view like this.

```
public class CommentView
{
   [ViewMapping(Key = true)]
   public int ID {get;set;}
   //Not using View Mapping attr here because the property name are the same
   public String Text{get;set;}
   [ViewMapping(ColumnPropertyName = "Comments", MaxDepth = 5)]
   public IEnumerable<CommentView> SubComments {get;set;}
}

```
You might be asking we need a max depth. The answer is when generating the expression to build the view, we have no way of determining the depth since the data has not been queried yet. The Mapper is stupid and would generate expression recursively forever simply because it doesn't know when to stop. The Max Depth should be set to correspond to your business rules for the depth of that particular tree. You might also use this to limit the amount of data within a recursive view i.e. maybe you only want to bring back two levels of your Comment Tree and then retrieve the rest via ajax on demand.

**OrderBy, Descending, OrderBySequenc** - As you might have guessed this allows you to determine a sort sequence for a list of View Models. Set `OrderBy = true` if you want to order the view by that property, set `Descending = true` if you want that ordering to be descending, and set the `OrderBySequence = 0-infinity 6/20/2013 1:51:26 PM (well almost infinity)` if you have multiple properties you want to order by.

**ReadOnly** - As simple as it sounds, a property can be mapped to your View Model, but will never map back to your Model.

**WriteOnly** - Again as simple as it sounds, a property can be mapped back to a model, but will never map from your model to view model.

**Type** - This may go away so use at your own risk. It is the ability to have a View that would have multiple mappings. You would Specify the Type of the Model here so the mapper can determine what ViewMapping attribute to use.

**UseParentListForRelationships** - This is for people using EntityFrameWork. In many cases the EntityFramework hides the fact that there is an association table linking two tables together. Set this equal to true if this is your case and the Mapper will find the correct model object to add to the Parent Models collection.

**Where** - Apply filters to lists with in your View. Say you application soft deletes records, you might apply a filter on a list of those records so the list does not contain any soft deleted records. The syntax would look something like this (look at the SubComments list):

```
public class CommentView
{
   [ViewMapping(Key = true)]
   public int ID {get;set;}
   //Not using View Mapping attr here because the property name are the same
   public String Text{get;set;}
   [ViewMapping(ColumnPropertyName = "Comments", MaxDepth = 5, Where="Deleted:=:false")]
   public IEnumerable<CommentView> SubComments {get;set;}
}
```

**Where Continued** - You can also now add dyanmic filters. To use thise prefix your Comparison Value with $ and pass in an objec that contains the filter. It would look something like this.

```

public class CommentView
{
   [ViewMapping(Key = true)]
   public int ID {get;set;}
   //Not using View Mapping attr here because the property name are the same
   public String Text{get;set;}
   [ViewMapping(ColumnPropertyName = "Comments", MaxDepth = 5, Where="Deleted:=:$ShowDeleted")]
   public IEnumerable<CommentView> SubComments {get;set;}
}

public class CommentFilter
{
   public Boolean ShowDeleted { get; set; }
}

  public void Run()
  {
     var commentFilter = new CommentFilter(){ShowDeleted = true};
     
     //The context here is assuemed to be whatever context you are using
     context.Comment.Map<Comment,CommentView>(commentFilter);
  }
```
*Note* Dynamic Comments prevent the expression tree from being cached so it must be build everytime.
*Note* If no filter object is passed in then the Dynamic filter will be ignored.

**ToBoolean** - This Allows you to convert Flags into the database directly to a Boolean. The syntax for this will be TrueValue:FalseValue e.g. Y:N. When mapping back the mapper will translate the Boolean back to the String it corresponds too.

**LinqFunction** - Allows you to call several LINQ function on the last property such as Sum and Count

###Automated Null Checks

One of the most common errors in Object Oriented Programming is the dreaded Object Not Set To Instance of Object Error. While this will not get ride of all your null checks it will automatically do the checks for you when mapping. The produced expression would look something like this.
```
public class Blog {
   public int ID {get;set;}
   public Person Author {get;set;}
   public String Text {get;set;}
}
public class Person{
   public int ID {get;set;}
   public String Name {get;set;}
}

public class BlogView {
   public int ID {get;set;}
   //This View Attribute is not necessary because of the auto mapping capabilities.
  [ViewMapping(ColumnPropertyName = "Author.Name")]
   public PersonName {get;set;}
   public String Text {get;set;}
}

//This Code:
var blog = Context.Blog.Find(1);
blog.Map<Blog, BlogView>();

//Generates this Expression
/*
Note we add the single object to a temporary list to generate a LINQ Mapping Function. 
Since we know that is the only item in the list we can call "list.Single()"
*/

blog.Select(blog => new BlogView(){
  ID = blog.ID,
  PersonName = Blog.Author!= null ? Blog.Person.Name : default(string),
  Text = blog.Text
}).Single();
```
###The View Filter Attribute

This is a class level attribute that takes in a Where string with the same syntax as the Where Property of the `ViewMappingAttribute`. Here is an example.

```
[ViewFilter("Deleted:=:true")]
public DeletedCommentView
{
   public String Name { get; set; }
   public Boolean Deleted { get; set; }
}
You can also apply a view filter to an Interface that your Views might implement.

[ViewFilter("Deleted:=:true")]
public interface IDeleted
{
   Boolean Deleted { get; set; }
}

public DeletedCommentView : IDeleted
{
   public String Name { get; set; }
   public Boolean Deleted { get; set; }
}
```
