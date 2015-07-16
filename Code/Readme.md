# Kerosene ORM 7.4.1

*Kerosene ORM* is a dynamic, configuration-less, self-adaptive, database first
and high performance ORM solution providing full support for POCO objects and
a SQL-like syntax from pure C#:

-	Configuration-less and self-adaptive: *Kerosene ORM* does not need to use any
external configuration of mapping files to define the structure of your databases
or how them map into your business entities, as all this discovery process is done
dynamically and at run time.

-	Full real support of POCO classes: your applications can define their business
entities the way it best fit into their needs, without needing them to inherit from
any base class of to implement any interfaces. Neither they have to be marked with
any ORM-related attributes, so they can be defines in independent assemblies with
no dependencies to the ORM ones.

-	No mandatory conventions: your entities are free to use the names they really
need for their properties, instead of having to follow spurious conventions for
the benefit of the ORM.

-	Flexible and resillient: *Kerosene ORM* is architected to endure changes in
the structure of the database or in the definition of your business entities
without needing recompilation, as far as the names of the relevant columns and
properties remaing the same and their types remain compatible. So you can add
or drop columns in the database, or modify the fields and properties of your
business entities, as your application evolves in time.

-	Full control of the SQL code generated: *Kerosene ORM* will translate into
SQL code only the expressions you have written, no more no less, and only those
will be executed. It will not inject additional code you have not specified, so
your application will have full control on the SQL code performance.