// Pragmatic client-side email check for instant feedback. The API's [EmailAddress] attribute is
// the authoritative validator; this only catches obviously-malformed input (e.g. "yash") before a
// pointless round trip. It is purely local, so it never reveals whether an account exists.
const EMAIL_PATTERN = /^\S+@\S+\.\S+$/

export function isValidEmail(value: string): boolean {
  return EMAIL_PATTERN.test(value.trim())
}
