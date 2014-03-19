GSharpTools, aka GTools
=======================

Generously provided FREE of charge by  [Gerson  Kurz][1]  via  his  [web
site][2].

This is the source code that is installed with the latest version of the
most excellent GTools package which is available for  [download here][3]
or, you can clone this repository and build it yourself.

Branches:
---------

 *  `master` - This is the source code as supplied by Mr. Kurz. The only
    modification is the addition of this README file. I will keep an eye
    on the web page and if Mr. Kurz makes  any  updates  they  will  get 
    merged in as quickly as I notice them.

 *  `dev` - This is where I will make  any  modifications  such  as  new 
    features or bug fixes. While I will endeavor to make sure this
    will build, there is no guarantee. I will not merge any updates I
    make into `master` unless/until [Mr. Kurz][1] signs off on them.
    Here's how that would probably work for a new feature.
    
    1.  I figure out a feature I'd like to see in the app.
    2.  I contact [Mr. Kurz][1] and discuss it with him.
    3.  He agrees that it would be a good feature to add.
    4.  I create a feature branch and begin working on it.
    5.  When I get it working based upon my understanding of whatever
        acceptance criteria [Mr. Kurz][1] has established for potential
        inslusion into the project such as coding standards, unit tests,
        etc.
    6.  I merge the feature branch into `dev` and submit the code for
        approval.
    7.  If he approves he merges my changes into the project source and
        publishes the update.
    8.  I update my master branch by downloading the updated source from
        the [GTools web site][2]. This ensures that my published `master`
        master branch remains consistent with the official project. I
        also back-merge the updates into `dev`.

    If the process doesn't go something like that, meaning that no new
    official update is issued, and I find the feature worth the effort of
    having to maintain two code bases, my copy of the official as well as
    my own private modified version, I'll keep the private feature branch
    as my new private `master`. I do not want to cause a public fork in
    the project.

[1]:    mailto:not@p-nand-q.com "Email Gerson Kurz"
[2]:    http://p-nand-q.com
[3]:    http://bit.ly/1c3W3j5 "GTools Download"
