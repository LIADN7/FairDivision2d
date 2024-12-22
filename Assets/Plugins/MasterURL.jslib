mergeInto(LibraryManager.library, {
    getMasterURL: function () {
        var masterURL = window.location != window.parent.location ? document.referrer : document.location.href;
        console.log("Heyyyyy ", masterURL);
        return masterURL;
    },
});
