# PT.SourceStats

Utility for statistics collection for different projects.
At present time C#, Java and PHP supported.

## Build Status, NuGet and Artifacts

[![PT.SourceStats Build Status](https://ci.appveyor.com/api/projects/status/vo0acpvek4q1x8yh?svg=true)](https://ci.appveyor.com/project/KvanTTT/pt-sourcestats)

The last nightly packages can be found here: [https://ci.appveyor.com/nuget/pt-sourcestats-4gails4hwlb6](https://ci.appveyor.com/nuget/pt-sourcestats-4gails4hwlb6).
Artifacts also [available](https://ci.appveyor.com/project/KvanTTT/pt-sourcestats/build/artifacts).

## Command Line Arguments

* **-f**, **--files** - path to directory for statistics collecting.
* **--mt** - multithread processing. By default: false.
* **--log-level** - log level. Available values:
    * **All** - progress + error + result messages (by default).
    * **Errors** - error + result messages.
    * **Result** - result messages only.
* **--start** - file index for scan files. Calculated for file name string array 
which returned from standart 
`Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);` method.
By default: 0.
* **--length** - number of scan files. By default: 0 - all files included in scan.
* **--out-dir** - directory of output PT.SourceStats.json file. By default not saved.
* **-v**, **--version** - version of **PT.PM** and **PT.SourceStats**
(usually they differs by build number, the last number).
* **?**, **--help** - print to console info about command line parameters.

## Features

### Java

* File count in project.
* Source code files count (.java, .class, .jsp): common + per extension.
* Number of source code lines (.java, .class, .jsp): commont + per extension.
* Wheather .xhtml files exists?
* Dependency plugins: maven, gradle, iby, ant.
* Build tools: ant, maven, gradle.
* Used libraries (jar) and their versions.
* CVE list.
* Config weakness.

### PHP

* composer.json, composer.lock and php.ini files content.
    * Invoke function names.
    * Include file names.
* Strings from .htaccess which starts from `php_value`, `php_flag`, `RewriteCond` and `RewriteRule`

### C\#

* Solution structure (sln and csproj files relative location).
* Projects list which included in solution (name and GUID).
* For every project:
    * GUID (see description in [List of Visual Studio Project Type GUIDs](http://www.codeproject.com/Reference/720512/List-of-Visual-Studio-Project-Type-GUIDs)).
    * Framework version.
    * References list.
    * Nuget dependencies.
    * Number of files.
    * Number of source code files (.cs, .aspx, .cshtml, .ashx, ascx): common + per extension.
    * CLOC (Count Lines of Code): common + per extension.
* CVE list.
* Config weakness.

## Message Format

JSON.NET is used for messages serialization/deserialization.

## Message Samples

### Progress Message

```JSON
{
  "MessageType": "Progress",
  "ProcessedCount": 134,
  "TotalCount": 557,
  "LastFileName": "Z:\\PHP\\dvwa\\external\\phpids\\0.6\\docs\\phpdocumentor\\blank.html"
}
```

### Error Message

```JSON
{
  "MessageType": "Error",
  "Message": "Parsing error in \"Z:\\PHP\\mutillidae\\includes\\header.php\": no viable alternative at input '<html>\\r\\n<head>\\r\\n\\t<meta content=\"text/html; charset=us-ascii\" http-equiv=\"content-type\">\\r\\n\\r\\n ... \\t\\t\\t\\t\\t\\t\\t\\t}else{\\r\\n\\t\\t\\t\\t\\t\\t\\t\\t<a href=\"#\">Setup/reset the DB (Disabled: Not Admin)</a></' at 591:65."
}
```

### Result Message

<details>
  <summary>Sample</summary>

```JSON
{
  "MessageType": "Result",
  "Directory": "C:\\Users\\User\\Documents\\Proj",
  "ErrorCount": 0,
  "LanguageStatistics": [
    {
      "Language": "Php",
      "FilesContent": {
        "C:\\Users\\User\\Documents\\Proj\\dvwa\\php.ini": "; This file attempts to overwrite the original php.ini file. Doesnt always work.\r\n\r\nmagic_quotes_gpc = Off\r\nallow_url_fopen on\r\nallow_url_include on"
      },
      "HtaccessStrings": [
        "php_flag magic_quotes_gpc Off",
        "php_flag magic_quotes_gpc Off"
      ],
      "ClassUsings": {
        "ids_monitor": 45,
        "ids_log_composite": 3,
        "intrusion": 1,
        "exception": 24,
        "pdo": 2,
        "pdoexception": 1,
        "invalidargumentexception": 7,
        "arrayobject": 2,
        "htmlpurifier": 3,
        "memcache": 1,
        "ids_filter": 24,
        "htmlpurifier_strategy_core": 1,
        "class": 7,
        "ids_filter_storage": 2,
        "ids_report": 5,
        "ids_event": 11,
        "htmlpurifier_childdef_required": 1,
        "htmlpurifier_attrdef_integer": 4,
        "tidy": 1,
        "reflectionmethod": 1,
        "htmlpurifier_attrdef_css_multiple": 6,
        "htmlpurifier_attrdef_css_composite": 13,
        "stdclass": 2,
        "htmlpurifier_varparser": 2,
        "htmlpurifier_attrtypes": 1,
        "htmlpurifier_doctyperegistry": 1,
        "module": 1,
        "csstidy": 1,
        "htmlpurifier_attrtransform_imgrequired": 1,
        "htmlpurifier_stringhash": 2,
        "htmlpurifier_configschema_interchange_namespace": 1,
        "phpunit_framework_testsuite": 1,
        "htmlpurifier_tokenfactory": 1,
        "domdocument": 2,
        "xml_htmlsax3": 1,
        "domdocumenttype": 1,
        "htmlpurifier_strategy_removeforeignelements": 1,
        "htmlpurifier_strategy_makewellformed": 1,
        "htmlpurifier_strategy_fixnesting": 1,
        "htmlpurifier_strategy_validateattributes": 1,
        "injector": 2
      },
      "MethodInvocations": {
        "define": 36,
        "dvwaphpidsversionget": 2,
        "array": 471,
        "dvwapagestartup": 22,
        "dvwapagenewgrab": 19,
        "dvwareadidslog": 1,
        "dvwaclearidslog": 1,
        "isset": 320,
        "array_key_exists": 4,
        "file_get_contents": 14,
        "preg_replace_callback": 10,
        "defined": 5,
        "in_array": 42,
        "setcookie": 3,
        "strip_tags": 8,
        "htmlspecialchars": 36,
        "pg_connect": 4,
        "mysql_fetch_row": 1,
        "file": 1,
        "explode": 58,
        "str_replace": 44,
        "urldecode": 3,
        "fopen": 7,
        "strpos": 43,
        "substr": 84,
        "join": 6,
        "trim": 55,
        "rawurlencode": 1,
        "ksort": 8,
        "rtrim": 16,
        "realpath": 1,
        "strtoupper": 4,
        "htmlpurifier_bootstrap::getpath": 1,
        "strncmp": 4,
        "spl_autoload_functions": 1,
        "spl_autoload_unregister": 1,
        "array_pop": 57,
        "set_error_handler": 4,
        "iconv": 5,
        "restore_error_handler": 7,
        "array_flip": 5,
        "parent::__construct": 5,
        "parent::validate": 10,
        "ctype_xdigit": 5,
        "is_float": 1,
        "is_bool": 4,
        "parent::offsetget": 1,
        "feof": 3,
        "fgets": 2,
        "ctype_digit": 6,
        "ctype_alpha": 4,
        "ctype_alnum": 4,
        "parent::getchilddef": 1,
        "pack": 1,
        "htmlentities": 4,
        "dvwahelphtmlecho": 1,
        "highlight_string": 4,
        "dvwasourcehtmlecho": 2,
        "array_diff": 1
      },
      "Includes": {
        "dvwa_web_page_to_root.dvwa/includes/dvwapage.inc.php": 22,
        "dvwa_web_page_to_root.dvwa/includes/dbms/mysql.php": 1,
        "dvwa_web_page_to_root.dvwa/includes/dbms/pgsql.php": 1,
        "ids/init.php": 7,
        "ids/log/file.php": 2,
        "ids/log/composite.php": 2,
        "dvwa_web_page_to_root.config/config.inc.php": 1,
        "dvwaphpids.inc.php": 1,
        "ids/caching/interface.php": 4,
        "path": 1,
        "htmlpurifier.php": 1,
        "htmlpurifier/attrcollections.php": 1,
        "htmlpurifier/bootstrap.php": 2,
        "htmlpurifier/definition.php": 1,
        "htmlpurifier/cssdefinition.php": 1,
        "htmlpurifier/childdef.php": 1,
        "ids/monitor.php": 3,
        "ids/filter/storage.php": 3,
        "ids/caching/factory.php": 3,
        "ids/filter.php": 4,
        "ids/log/interface.php": 4,
        "ids/report.php": 3,
        "ids/event.php": 4,
        "ids/converter.php": 1,
        "htmlpurifier.autoload.php": 1,
        "htmlpurifier_prefix./.file": 1,
        "filename": 1,
        "phpunit/framework/testsuite.php": 1,
        "phpunit/textui/testrunner.php": 1,
        "phpunit/util/filter.php": 1,
        "ids/monitortest.php": 1,
        "ids/reporttest.php": 1,
        "ids/inittest.php": 1,
        "ids/exceptiontest.php": 1,
        "ids/filtertest.php": 1,
        "ids/cachingtest.php": 1,
        "ids/eventtest.php": 1,
        "phpunit/framework/testcase.php": 7,
      }
    },
    {
      "Language": "Java",
      "FilesCount": 5938,
      "SourceFilesCount": 1258,
      "JavaFilesCount": 462,
      "ClassFilesCount": 632,
      "JspFilesCount": 164,
      "JavaLinesCount": 122499,
      "SourceCodeLinesCount": 178975,
      "ClassLinesCount": 45413,
      "JspLinesCount": 11063,
      "XHtmlFileCount": 0,
      "DependencyManagers": [
        "maven2-repository.dev.java.net http://download.java.net/maven/2"
      ],
      "BuildTools": [
        "maven-compiler-plugin-",
        "maven-eclipse-plugin-",
        "tomcat-maven-plugin-"
      ],
      "Dependencies": [
        "mail-1.4.2",
        "mailapi-1.4.2",
        "wsdl4j-1.5.1",
        "activation-1.1",
        "axis-1.2",
        "axis-ant-1.2",
        "axis-jaxrpc-1.2",
        "axis-saaj-1.2",
        "catalina-4.1.9",
        "commons-beanutils-1.6",
        "commons-collections-3.1",
        "commons-digester-1.4.1",
        "commons-discovery-0.2",
        "commons-fileupload-1.2.1",
        "commons-io-1.4",
        "commons-logging-1.0.4",
        "ecs-1.4.2",
        "hsqldb-1.8.0.7",
        "j2h-1.3.1",
        "jta-1.0.1B",
        "jtds-1.2.2",
        "log4j-1.2.8",
        "servlet-api-2.3",
        "tomcat-catalina-7.0.27"
      ]
    },
    {
      "Language": "CSharp"
    }
  ]
}
```

</details>