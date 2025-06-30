# How to make a Location Checklist

## Autocomplete

locations and keywords will have autocomplete, so you won't need to type the entire location, just enough to narrow it down. Adding a space my mess up the autocomplete, so be wary of that

## Keywords

- `location`
- `note`
- `openfolder`
- `closefolder`

comment symbol is `#`, anything on a line starting with `#` will be ignored
commenting will only work at the start of a line

## Strictness

the custom 'language' for checklists is not very strict

- indentation will be ignored so organize it how you wish
- commenting lines is only required if you don't want to include information in a note, as long as the line doesn't start with a keyword it will be ignored
- for notes any line under it will be added unless it is commented out
  - if there is a keyword the note will stop being added to

## How to insert a location

```
location [name of location]
```

replace [name of location] with the location you want, there will be autofill so just start typing after `location `

### Adding a note to a location

just add `note` under it like this

```
locaiton [name of location]
    note [information about the location]
        this line will be added to the note above
        same for this one
```

## Creating folders

folders can help you keep track and organize the checklist, and they are easy to make

```
openfolder [folder name]
    location [name of location]
closefolder
```

the last chain of `closefolder` isn't required

valid:
```
openfolder [main folder]
    openfolder [sub region 1]
        location [name of location a]
    closefolder
    openfolder [sub region 2]
        location [name of location b]
    closefolder
closefolder
```

also valid:
```
openfolder [main folder]
    openfolder [sub region 1]
        location [name of location a]
    closefolder
    openfolder [sub region 2]
        location [name of location b]
```